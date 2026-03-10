#!/usr/bin/env bash
# validate-symlinks.sh
# Validates that AI instruction symlinks exist and point to CLAUDE.md.
# Handles both real symlinks (Linux/macOS) and Windows text-symlinks
# (git core.symlinks=false checks out symlinks as text files).
#
# Usage:
#   bash scripts/validate-symlinks.sh
#
# Exit codes:
#   0 — all symlinks valid
#   1 — one or more broken, missing, or mis-targeted

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Format: "link:expected_target"
LINKS=(
  "AGENTS.md:CLAUDE.md"
  "CODEX.md:CLAUDE.md"
  ".github/copilot-instructions.md:../CLAUDE.md"
  ".github/AGENTS.md:../CLAUDE.md"
)

FAILED=0

check_link() {
  local link="$1"
  local expected="$2"

  if [ -L "$link" ]; then
    # Real symlink (Linux / macOS / Windows with core.symlinks=true)
    local actual
    actual=$(readlink "$link")
    if [ "$actual" != "$expected" ]; then
      echo "  FAIL: $link -> $actual  (expected: $expected)"
      FAILED=1
    elif [ ! -e "$link" ]; then
      echo "  FAIL: $link is a symlink but the target does not exist"
      FAILED=1
    else
      echo "  OK  : $link -> $actual"
    fi

  elif [ -f "$link" ]; then
    # Text-symlink fallback (Windows, git core.symlinks=false)
    local content
    content=$(cat "$link" | tr -d '\r\n')
    if [ "$content" != "$expected" ]; then
      echo "  FAIL: $link is a plain file with content '$content'  (expected symlink to: $expected)"
      FAILED=1
    else
      # Verify the target actually resolves
      local dir target
      dir=$(dirname "$link")
      target="$dir/$content"
      if [ ! -f "$target" ] && [ ! -L "$target" ]; then
        echo "  FAIL: $link text-symlink target not found: $target"
        FAILED=1
      else
        echo "  OK  : $link -> $content  (text symlink)"
      fi
    fi

  else
    echo "  FAIL: $link is missing"
    FAILED=1
  fi
}

echo "Validating AI instruction symlinks..."
echo ""

for entry in "${LINKS[@]}"; do
  link="${entry%%:*}"
  expected="${entry##*:}"
  check_link "$link" "$expected"
done

echo ""

if [ "$FAILED" -eq 1 ]; then
  echo "One or more symlinks are broken. Recreate with:"
  echo ""
  echo "  ln -sf CLAUDE.md AGENTS.md"
  echo "  ln -sf CLAUDE.md CODEX.md"
  echo "  ln -sf ../CLAUDE.md .github/copilot-instructions.md"
  echo "  ln -sf ../CLAUDE.md .github/AGENTS.md"
  exit 1
fi

echo "All AI instruction symlinks are valid."
