#!/usr/bin/env bash
# Decides whether a change can affect the .NET build, tests or CodeQL scan, and writes dotnet=true|false to
# $GITHUB_OUTPUT. Used by ci.yml and codeql.yml so the list of relevant paths lives in one place.
# Anything unclear (schedule, manual run, missing commit, diff error) resolves to true: running too much is
# harmless, skipping a needed run is not. Run with `bash`; needs a full-history checkout (fetch-depth: 0).
#
# Environment: EVENT_NAME, BASE_SHA and HEAD_SHA (pull_request), BEFORE_SHA and AFTER_SHA (push).

RELEVANT='^(src/|Notary\.AppHost/|Notary\.ServiceDefaults/|\.github/workflows/(ci|codeql)\.yml$|\.github/scripts/detect-dotnet-changes\.sh$)|(^|/)(Directory\..*\.(props|targets)|global\.json|nuget\.config|\.editorconfig|\.globalconfig)$'

emit()
{
    echo "dotnet=$1" >> "${GITHUB_OUTPUT:-/dev/stdout}"
    echo "dotnet=$1 ($2)"
    exit 0
}

case "${EVENT_NAME:-}" in
    pull_request)
        range="${BASE_SHA:-}...${HEAD_SHA:-}"
        ;;
    push)
        range="${BEFORE_SHA:-}..${AFTER_SHA:-}"
        ;;
    *)
        emit true "event '${EVENT_NAME:-}' always runs"
        ;;
esac

if ! files=$(git diff --name-only "$range" 2>/dev/null); then
    emit true "could not diff $range"
fi

if [ -z "$files" ]; then
    emit true "empty diff for $range"
fi

if matched=$(printf '%s\n' "$files" | grep -iE "$RELEVANT" | head -5) && [ -n "$matched" ]; then
    emit true "relevant: $(echo "$matched" | tr '\n' ' ')"
fi

emit false "no .NET inputs among $(printf '%s\n' "$files" | wc -l) changed files"
