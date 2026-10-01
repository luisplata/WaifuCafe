#!/bin/bash
set -euo pipefail
# Free disk space on GitHub-hosted ubuntu runners (GameCI pattern)
sudo rm -rf /usr/share/dotnet
sudo rm -rf /usr/local/lib/android
sudo rm -rf /opt/ghc
sudo apt-get clean
df -h