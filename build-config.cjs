module.exports = {
  // companion-module-build copies extraFiles to the root of the packaged module.
  // The Windows helper publish output is intentionally included as a flat set so
  // the .NET apphost can load its adjacent runtime/projection files.
  extraFiles: ['runtime/win-x64/*'],
}
