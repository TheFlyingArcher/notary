const test = require('node:test');
const assert = require('node:assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { run } = require('../check-fire-state.cjs');

const STATE = `project:
  name: demo
intents:
  - id: alpha
    title: Alpha
    status: STATUS_ALPHA
    COMPLETED_ALPHA
    work_items:
      - id: a-one
        title: One
        status: completed
        completed_at: 2026-10-01T00:00:00.000Z
      - id: a-two
        title: Two
        status: completed
        completed_at: 2026-10-02T00:00:00.000Z
  - id: beta
    title: Beta
    status: in_progress
    work_items:
      - id: b-one
        title: One
        status: completed
        completed_at: 2026-10-03T00:00:00.000Z
      - id: b-two
        title: Two
        status: pending
runs:
  active: []
`;

function brief(id, status, completedAt)
{
    const completed = completedAt ? `completed_at: ${completedAt}\n` : '';
    return `---\nid: ${id}\ntitle: ${id}\nstatus: ${status}\ncreated: 2026-09-30T00:00:00Z\n${completed}---\n\n# Intent\n\nbody status: pending\n`;
}

function item(id, status)
{
    return `---\nid: ${id}\ntitle: ${id}\nmode: confirm\nstatus: ${status}\ndepends_on: []\n---\n\n# Item\n`;
}

/** Builds a repository with a consistent FIRE state; "tweak" may break it. */
function makeRepo(tweak)
{
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'fire-state-'));
    const files = {
        '.specs-fire/state.yaml': STATE.replace('STATUS_ALPHA', 'completed').replace('COMPLETED_ALPHA', 'completed_at: 2026-10-02T00:00:00.000Z'),
        '.specs-fire/intents/alpha/brief.md': brief('alpha', 'completed', '2026-10-02T00:00:00.000Z'),
        '.specs-fire/intents/alpha/work-items/a-one.md': item('a-one', 'completed'),
        '.specs-fire/intents/alpha/work-items/a-two.md': item('a-two', 'completed'),
        '.specs-fire/intents/beta/brief.md': brief('beta', 'in_progress'),
        '.specs-fire/intents/beta/work-items/b-one.md': item('b-one', 'completed'),
        '.specs-fire/intents/beta/work-items/b-two.md': item('b-two', 'pending'),
    };
    if (tweak)
    {
        tweak(files);
    }
    for (const [relative, text] of Object.entries(files))
    {
        const target = path.join(root, relative);
        fs.mkdirSync(path.dirname(target), { recursive: true });
        fs.writeFileSync(target, text);
    }
    return root;
}

function snapshot(root)
{
    const result = {};
    const walk = directory =>
    {
        for (const entry of fs.readdirSync(directory, { withFileTypes: true }))
        {
            const full = path.join(directory, entry.name);
            if (entry.isDirectory())
            {
                walk(full);
            }
            else
            {
                result[path.relative(root, full).replace(/\\/g, '/')] = fs.readFileSync(full, 'utf8');
            }
        }
    };
    walk(root);
    return result;
}

/** Runs the check, capturing console output. */
function check(root, fix)
{
    const lines = [];
    const original = console.log;
    console.log = (...args) => lines.push(args.join(' '));
    try
    {
        const code = run({ root, fix: Boolean(fix) });
        return { code, output: lines.join('\n') };
    }
    finally
    {
        console.log = original;
    }
}

/** Lines that differ between two snapshots, as "file: old -> new" */
function changedLines(before, after)
{
    const changes = [];
    for (const file of Object.keys(after))
    {
        if (before[file] === after[file])
        {
            continue;
        }
        const oldLines = before[file].split('\n');
        const newLines = after[file].split('\n');
        const removed = oldLines.filter(line => !newLines.includes(line));
        const added = newLines.filter(line => !oldLines.includes(line));
        changes.push({ file, removed, added });
    }
    return changes;
}

const CASES = {
    'intent completed in files but in_progress in state': {
        tweak: files => { files['.specs-fire/state.yaml'] = files['.specs-fire/state.yaml'].replace('status: completed\n    completed_at: 2026-10-02T00:00:00.000Z', 'status: in_progress'); },
        expect: 'intent alpha status expected \'completed\' found \'in_progress\'',
    },
    'intent completed in state but an item is not': {
        tweak: files => { files['.specs-fire/state.yaml'] = files['.specs-fire/state.yaml'].replace('id: a-two\n        title: Two\n        status: completed', 'id: a-two\n        title: Two\n        status: in_progress'); },
        expect: 'intent alpha status expected \'in_progress\' found \'completed\'',
    },
    'brief out of date': {
        tweak: files => { files['.specs-fire/intents/beta/brief.md'] = brief('beta', 'completed', '2026-10-03T00:00:00.000Z'); },
        expect: '.specs-fire/intents/beta/brief.md: status expected \'in_progress\' found \'completed\'',
    },
    'work item file out of date': {
        tweak: files => { files['.specs-fire/intents/beta/work-items/b-one.md'] = item('b-one', 'pending'); },
        expect: '.specs-fire/intents/beta/work-items/b-one.md: status expected \'completed\' found \'pending\'',
    },
    'completed brief without completed_at': {
        tweak: files => { files['.specs-fire/intents/alpha/brief.md'] = brief('alpha', 'completed'); },
        expect: '.specs-fire/intents/alpha/brief.md: completed_at expected \'a timestamp\' found \'missing\'',
    },
    'completed intent without completed_at in state': {
        tweak: files => { files['.specs-fire/state.yaml'] = files['.specs-fire/state.yaml'].replace('    completed_at: 2026-10-02T00:00:00.000Z\n    work_items', '    work_items'); },
        expect: 'intent alpha completed_at expected \'a timestamp\' found \'missing\'',
    },
};

test('a consistent repository passes', () =>
{
    const root = makeRepo();
    const result = check(root);
    assert.strictEqual(result.code, 0);
    assert.match(result.output, /consistent/);
});

test('an intent with no completed items may stay pending', () =>
{
    const root = makeRepo(files =>
    {
        files['.specs-fire/state.yaml'] = files['.specs-fire/state.yaml'].replace('b-one\n        title: One\n        status: completed\n        completed_at: 2026-10-03T00:00:00.000Z', 'b-one\n        title: One\n        status: pending');
        files['.specs-fire/intents/beta/work-items/b-one.md'] = item('b-one', 'pending');
        files['.specs-fire/state.yaml'] = files['.specs-fire/state.yaml'].replace('id: beta\n    title: Beta\n    status: in_progress', 'id: beta\n    title: Beta\n    status: pending');
        files['.specs-fire/intents/beta/brief.md'] = brief('beta', 'pending');
    });
    assert.strictEqual(check(root).code, 0);
});

for (const [name, definition] of Object.entries(CASES))
{
    test(`fails: ${name}`, () =>
    {
        const root = makeRepo(definition.tweak);
        const result = check(root);
        assert.strictEqual(result.code, 1);
        assert.ok(result.output.includes(definition.expect), `missing "${definition.expect}" in:\n${result.output}`);
        assert.match(result.output, /Fix with: node \.github\/scripts\/check-fire-state\.cjs --fix/);
    });

    test(`--fix repairs and only touches status lines: ${name}`, () =>
    {
        const root = makeRepo(definition.tweak);
        const before = snapshot(root);
        assert.strictEqual(check(root, true).code, 0);
        assert.strictEqual(check(root).code, 0, 'second run should pass');

        for (const change of changedLines(before, snapshot(root)))
        {
            for (const line of [...change.removed, ...change.added])
            {
                assert.match(line.trim(), /^(status|completed_at):/, `${change.file} changed an unrelated line: ${line}`);
            }
        }
    });
}

test('--fix keeps CRLF line endings', () =>
{
    const root = makeRepo(files =>
    {
        files['.specs-fire/intents/beta/work-items/b-one.md'] = item('b-one', 'pending').replace(/\n/g, '\r\n');
    });
    assert.strictEqual(check(root, true).code, 0);
    const text = fs.readFileSync(path.join(root, '.specs-fire/intents/beta/work-items/b-one.md'), 'utf8');
    assert.ok(text.includes('status: completed\r\n'));
    assert.ok(!/[^\r]\n/.test(text), 'every line ending should still be CRLF');
});

test('--fix adds a missing completed_at to the brief after created', () =>
{
    const root = makeRepo(CASES['completed brief without completed_at'].tweak);
    check(root, true);
    const text = fs.readFileSync(path.join(root, '.specs-fire/intents/alpha/brief.md'), 'utf8');
    assert.match(text, /created: 2026-09-30T00:00:00Z\ncompleted_at: 2026-10-02T00:00:00\.000Z\n---/);
});

test('--fix leaves the rest of state.yaml untouched', () =>
{
    const root = makeRepo(CASES['intent completed in files but in_progress in state'].tweak);
    check(root, true);
    const state = fs.readFileSync(path.join(root, '.specs-fire/state.yaml'), 'utf8');
    assert.match(state, /project:\n  name: demo\n/);
    assert.match(state, /runs:\n  active: \[\]\n/);
});
