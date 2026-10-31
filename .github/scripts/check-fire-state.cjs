#!/usr/bin/env node
/**
 * Checks that the FIRE bookkeeping agrees with itself.
 *
 *   node .github/scripts/check-fire-state.cjs [--fix] [--root <dir>]
 *
 * Rule 1 (state.yaml): an intent whose work items are all completed is completed and has completed_at; any other intent
 *         is not completed (in_progress, or pending when every item is pending).
 * Rule 2 (files): every brief.md has the intent's status and every work item file has its item's status from
 *         state.yaml; a completed brief has completed_at.
 *
 * Prints every mismatch and exits 1. With --fix it corrects status and completed_at values only, leaving every other
 * line and the line endings alone. state.yaml is the source of truth for work item statuses.
 * Needs the "yaml" package (NODE_PATH or node_modules).
 */
const fs = require('fs');
const path = require('path');
const YAML = require('yaml');

const FIX_COMMAND = 'node .github/scripts/check-fire-state.cjs --fix';

function parseArgs(argv)
{
    const options = { fix: false, root: process.cwd() };
    for (let i = 0; i < argv.length; i++)
    {
        if (argv[i] === '--fix')
        {
            options.fix = true;
        }
        else if (argv[i] === '--root' && argv[i + 1])
        {
            options.root = path.resolve(argv[++i]);
        }
        else
        {
            throw new Error(`Unknown argument: ${argv[i]}`);
        }
    }
    return options;
}

function frontMatterRange(text)
{
    const match = /^---\r?\n([\s\S]*?)\r?\n---(\r?\n|$)/.exec(text);
    if (!match)
    {
        return null;
    }
    return { start: match[0].indexOf(match[1]), body: match[1] };
}

function readField(text, field)
{
    const range = frontMatterRange(text);
    if (!range)
    {
        return null;
    }
    const match = new RegExp(`^${field}:[ \\t]*(.*?)[ \\t]*\\r?$`, 'm').exec(range.body);
    return match ? match[1] : null;
}

/** Sets a front matter field, adding it after the "after" field (or at the end of the block) when it is missing. */
function writeField(text, field, value, after)
{
    const range = frontMatterRange(text);
    if (!range)
    {
        return text;
    }
    const eol = text.includes('\r\n') ? '\r\n' : '\n';
    const existing = new RegExp(`^(${field}:[ \\t]*)(.*?)([ \\t]*\\r?)$`, 'm');
    let body = range.body;
    if (existing.test(body))
    {
        body = body.replace(existing, (m, head, old, tail) => `${head}${value}${tail}`);
    }
    else
    {
        const lines = body.split(/\r?\n/);
        const index = lines.findIndex(line => line.startsWith(`${after}:`));
        lines.splice(index >= 0 ? index + 1 : lines.length, 0, `${field}: ${value}`);
        body = lines.join(eol);
    }
    return text.slice(0, range.start) + body + text.slice(range.start + range.body.length);
}

function expectedIntentStatus(intent)
{
    const statuses = intent.work_items.map(item => item.status);
    if (statuses.every(status => status === 'completed'))
    {
        return 'completed';
    }
    return statuses.every(status => status === 'pending') && intent.status === 'pending' ? 'pending' : 'in_progress';
}

function latestCompletion(intent)
{
    return intent.work_items.map(item => item.completed_at).filter(Boolean).map(String).sort().pop();
}

function run(options)
{
    const statePath = path.join(options.root, '.specs-fire', 'state.yaml');
    const document = YAML.parseDocument(fs.readFileSync(statePath, 'utf8'));
    const state = document.toJS();
    const mismatches = [];
    const fileEdits = [];
    let stateChanged = false;

    const report = (file, field, expected, found) => mismatches.push({ file, field, expected, found });
    const relative = file => file.replace(/\\/g, '/');

    (state.intents || []).forEach((intent, index) =>
    {
        if (!Array.isArray(intent.work_items) || intent.work_items.length === 0)
        {
            return;
        }

        const briefFile = path.join('.specs-fire', 'intents', intent.id, 'brief.md');
        const briefPath = path.join(options.root, briefFile);
        const briefText = fs.existsSync(briefPath) ? fs.readFileSync(briefPath, 'utf8') : null;
        const expected = expectedIntentStatus(intent);
        const node = document.getIn(['intents', index]);

        if (intent.status !== expected)
        {
            report('.specs-fire/state.yaml', `intent ${intent.id} status`, expected, intent.status);
            node.set('status', expected);
            stateChanged = true;
        }

        let completedAt = intent.completed_at ? String(intent.completed_at) : null;
        if (expected === 'completed' && !completedAt)
        {
            report('.specs-fire/state.yaml', `intent ${intent.id} completed_at`, 'a timestamp', 'missing');
            completedAt = (briefText && readField(briefText, 'completed_at')) || latestCompletion(intent) || new Date().toISOString();
            node.set('completed_at', completedAt);
            stateChanged = true;
        }

        if (briefText !== null)
        {
            let text = briefText;
            const briefStatus = readField(briefText, 'status');
            if (briefStatus !== expected)
            {
                report(relative(briefFile), 'status', expected, briefStatus);
                text = writeField(text, 'status', expected, 'id');
            }
            if (expected === 'completed' && !readField(text, 'completed_at'))
            {
                report(relative(briefFile), 'completed_at', 'a timestamp', 'missing');
                text = writeField(text, 'completed_at', completedAt, 'created');
            }
            if (text !== briefText)
            {
                fileEdits.push({ file: briefPath, text });
            }
        }

        intent.work_items.forEach(item =>
        {
            const itemFile = path.join('.specs-fire', 'intents', intent.id, 'work-items', `${item.id}.md`);
            const itemPath = path.join(options.root, itemFile);
            if (!fs.existsSync(itemPath))
            {
                return;
            }
            const itemText = fs.readFileSync(itemPath, 'utf8');
            const itemStatus = readField(itemText, 'status');
            if (itemStatus !== item.status)
            {
                report(relative(itemFile), 'status', item.status, itemStatus);
                fileEdits.push({ file: itemPath, text: writeField(itemText, 'status', item.status, 'mode') });
            }
        });
    });

    if (mismatches.length === 0)
    {
        console.log('FIRE state is consistent.');
        return 0;
    }

    for (const m of mismatches)
    {
        console.log(`${m.file}: ${m.field} expected '${m.expected}' found '${m.found}'`);
    }

    if (!options.fix)
    {
        console.log(`\n${mismatches.length} mismatch(es). Fix with: ${FIX_COMMAND}`);
        return 1;
    }

    if (stateChanged)
    {
        fs.writeFileSync(statePath, document.toString({ lineWidth: 0 }));
    }
    for (const edit of fileEdits)
    {
        fs.writeFileSync(edit.file, edit.text);
    }
    console.log(`\nFixed ${mismatches.length} mismatch(es).`);
    return 0;
}

if (require.main === module)
{
    try
    {
        process.exitCode = run(parseArgs(process.argv.slice(2)));
    }
    catch (error)
    {
        console.error(`check-fire-state: ${error.message}`);
        process.exitCode = 2;
    }
}

module.exports = { run, parseArgs };
