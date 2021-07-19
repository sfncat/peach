@echo off

Setlocal
set PYTHON=python
@%PYTHON% -x "%~dp0waf" %*
@exit /b %ERRORLEVEL%
