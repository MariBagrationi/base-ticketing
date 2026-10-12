#!/bin/sh
# Writes the ABI of every deployable contract to ./abi. The C# bindings in
# src/backend/TixFlow/TixFlow.Infrastructure/Contracts are generated from these files.
set -e
cd "$(dirname "$0")/.."
mkdir -p abi
for name in EventTicket TicketFactory EventEscrow PriceCappedResale SignatureValidator MockUSDC; do
  forge inspect "$name" abi --json > "abi/$name.json"
  echo "abi/$name.json"
done
