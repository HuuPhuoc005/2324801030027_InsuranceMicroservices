#!/usr/bin/env sh
set -eu
cd "$(dirname "$0")/.."
dotnet build InsuranceMicroservices.slnx -m:1 -nr:false
