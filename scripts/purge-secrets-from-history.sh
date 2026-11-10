#!/usr/bin/env bash
# Rewrites git history to remove leaked secrets. Contains NO secret values: only path globs and regexes.
#
# Usage (run on a FRESH MIRROR CLONE, never on your working copy):
#   git clone --mirror <repo-url-or-path> bonddy-clean.git
#   ./scripts/purge-secrets-from-history.sh bonddy-clean.git
#
# Requires git-filter-repo (pip install git-filter-repo). Without Python you can run it in Docker:
#   docker run --rm -v "$PWD:/w" -w /w python:3.12-alpine sh -c \
#     "apk add -q git bash && pip install -q git-filter-repo && bash scripts/purge-secrets-from-history.sh bonddy-clean.git"
set -euo pipefail

REPO="${1:?Usage: $0 <path-to-mirror-clone>}"
RULES="$(mktemp)"
trap 'rm -f "$RULES"' EXIT

# git-filter-repo --replace-text rules (Python regex on file contents, every blob in every commit).
cat > "$RULES" <<'RULES_EOF'
regex:("(?:Password|Secret|ApiKey|ChecksumKey|ClientSecret|PrivateKey)"\s*:\s*")[^"]+(")==>\1***REMOVED***\2
regex:("PayOS"\s*:\s*\{\s*"ClientId"\s*:\s*")[^"]+(")==>\1***REMOVED***\2
regex:((?<![A-Za-z_])Password=)(?![$&{])[^;"'\s]+==>\1***REMOVED***
regex:((?:JWT_SECRET|POSTGRES_PASSWORD|SMTP_PASSWORD|PAYPAL_SECRET|PAYPAL_CLIENT_ID|PAYPAL_WEBHOOK_ID|PAYOS_CLIENT_ID|PAYOS_API_KEY|PAYOS_CHECKSUM_KEY)=)(?!change-me\b)[^\s$]+==>\1***REMOVED***
RULES_EOF

cd "$REPO"
git -c safe.directory='*' filter-repo --force \
  --invert-paths \
  --path-glob '*firebase-adminsdk*.json' \
  --replace-text "$RULES"

echo "History rewritten. Verify, then force-push (see README / instructions)."
