@echo off
rem Builds the documentation site in docs\ into .build\ with Neko. The docs workflow
rem (.github/workflows/docs.yml) runs build.sh; this is the same for Windows.
rem   Install the tool once with: dotnet tool update --global Neko
cd /d "%~dp0"
neko build -i docs -o .build %*
