#!/bin/sh
# Installs the pinned Solidity dependencies into ./lib (which is git-ignored).
set -e
cd "$(dirname "$0")/.."
if [ -d lib/forge-std ] && [ -d lib/openzeppelin-contracts ] && [ -d lib/openzeppelin-contracts-upgradeable ]; then
  echo "dependencies already installed"
  exit 0
fi
forge install --no-git --shallow \
  foundry-rs/forge-std@v1.9.7 \
  OpenZeppelin/openzeppelin-contracts@v5.4.0 \
  OpenZeppelin/openzeppelin-contracts-upgradeable@v5.4.0
