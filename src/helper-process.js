const { spawn } = require('node:child_process')
const path = require('node:path')
const readline = require('node:readline')

class HelperProcess {
  constructor(self) {
    this.self = self
    this.proc = null
    this.rl = null
  }

  start(endpointName, releaseMs) {
    this.stop()

    if (process.platform !== 'win32') {
      throw new Error('This prototype currently supports Windows only.')
    }

    const exe = path.join(__dirname, 'ProToolsHuiBridge.exe')
    this.proc = spawn(exe, ['--endpoint', endpointName, '--release-ms', String(releaseMs)], {
      windowsHide: true,
      stdio: ['pipe', 'pipe', 'pipe'],
    })

    this.proc.on('error', (error) => {
      this.self.handleHelperError(error)
    })

    this.proc.on('exit', (code, signal) => {
      this.self.handleHelperExit(code, signal)
      this.proc = null
    })

    this.proc.stderr.setEncoding('utf8')
    this.proc.stderr.on('data', (chunk) => {
      for (const line of String(chunk).split(/\r?\n/)) {
        if (line.trim()) this.self.log('debug', `helper: ${line.trim()}`)
      }
    })

    this.rl = readline.createInterface({ input: this.proc.stdout })
    this.rl.on('line', (line) => {
      try {
        const msg = JSON.parse(line)
        this.self.handleHelperMessage(msg)
      } catch (error) {
        this.self.log('warn', `Invalid helper JSON: ${line}`)
      }
    })
  }

  send(payload) {
    if (!this.proc || !this.proc.stdin || this.proc.killed) {
      throw new Error('Windows MIDI helper is not running.')
    }
    this.proc.stdin.write(JSON.stringify(payload) + '\n')
  }

  stop() {
    if (this.rl) {
      this.rl.close()
      this.rl = null
    }

    if (this.proc) {
      try {
        if (this.proc.stdin && !this.proc.killed) {
          this.proc.stdin.write(JSON.stringify({ cmd: 'shutdown' }) + '\n')
        }
      } catch (_) {}

      setTimeout(() => {
        try {
          if (this.proc && !this.proc.killed) this.proc.kill()
        } catch (_) {}
      }, 500).unref()

      this.proc = null
    }
  }
}

module.exports = HelperProcess
