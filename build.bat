@echo off
rem Builds the documentation site in docs\ into .build\ with Neko. The docs pipeline
rem (.devops/build-docs.yml) runs this; build.sh is the same for Linux and macOS.
rem   Install the tool once with: dotnet tool update --global Neko
cd /d "%~dp0"
neko build -i docs -o .build %*
