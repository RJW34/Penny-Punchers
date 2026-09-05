#!/usr/bin/env python3
"""Measure movement/input/freeze clocks from the exact frozen production Core DLL."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
from xml.sax.saxutils import escape
from package_verification import ROOT, EXPECTED, run, sha


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet')
    parser.add_argument('--data',type=Path,default=ROOT/'data')
    parser.add_argument('--evidence-dir',type=Path,default=ROOT/'reports/evidence/feel-measurements')
    args=parser.parse_args()
    dotnet=args.dotnet or json.loads((ROOT/'.local_tools.json').read_text())['dotnet']
    core=ROOT/'src/StrikeLedger.Core/bin/ExportRelease/net8.0/StrikeLedger.Core.dll'
    if sha(core)!=EXPECTED['Core']:raise RuntimeError('Production Core identity differs from frozen shipment')
    evidence=args.evidence_dir.resolve();staging=evidence/'harness';staging.mkdir(parents=True,exist_ok=True)
    source=ROOT/'tools/FeelMeasurementHarness.cs'
    project=staging/'FeelMeasurements.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors><Optimize>true</Optimize><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="'+escape(str(source))+'"/><Reference Include="StrikeLedger.Core"><HintPath>'+escape(str(core))+'</HintPath><Private>true</Private></Reference></ItemGroup></Project>\n',encoding='utf-8')
    commands=[[str(dotnet),'build',str(project),'--configuration','ExportRelease','--nologo','--verbosity','minimal'],[str(dotnet),str(staging/'bin/ExportRelease/net8.0/FeelMeasurements.dll'),str(args.data.resolve()),str(evidence)]]
    try:
        run(commands[0],evidence/'build.log',180)
        run(commands[1],evidence/'measurements.log',180)
        report=json.loads((evidence/'feel-measurements.json').read_text())
        if not report['passed'] or report['coreBinarySha256']!=EXPECTED['Core']:raise RuntimeError('Measurement report identity or assertions failed')
        (evidence/'process.log').write_text('ACTUAL FROZEN CORE MEASUREMENTS\n'+json.dumps({'commands':commands,'coreSha256':sha(core),'harnessSourceSha256':sha(source),'reportSha256':sha(evidence/'feel-measurements.json'),'measurements':len(report['measurements']),'reproduce':'python tools/measure_core_feel.py'},indent=2)+'\nNo production source or project was compiled or modified. Per-step traces are in feel-measurements.json. Discrete fixed60 timing does not measure rendered frame pacing.\n',encoding='utf-8')
        print('PASS',len(report['measurements']),'actual core measurements;',evidence/'feel-measurements.json')
    finally:
        if sha(core)!=EXPECTED['Core']:raise RuntimeError('Frozen production Core changed during measurement')


if __name__=='__main__':main()
