const { InstanceBase, InstanceStatus, runEntrypoint } = require('@companion-module/base')
const HelperProcess = require('./helper-process')
const UpdateActions = require('./actions')
const UpdateFeedbacks = require('./feedbacks')
const UpdateVariables = require('./variables')
const UpgradeScripts = require('./upgrades')

class ProToolsHuiVirtualInstance extends InstanceBase {
  constructor(internal) {
    super(internal)
    this.helper = new HelperProcess(this)
    this.trackMute = Array(8).fill(null)
    this.proToolsConnected = false
    this.endpointName = 'Companion Pro Tools HUI'
  }

  async init(config) {
    this.config = config
    this.endpointName = String(config.endpointName || 'Companion Pro Tools HUI').trim() || 'Companion Pro Tools HUI'
    this.updateActions()
    this.updateFeedbacks()
    this.updateVariableDefinitions()
    this.startHelper()
  }

  async destroy() {
    this.helper.stop()
  }

  async configUpdated(config) {
    this.config = config
    this.endpointName = String(config.endpointName || 'Companion Pro Tools HUI').trim() || 'Companion Pro Tools HUI'
    this.startHelper()
  }

  getConfigFields() {
    return [
      {
        type: 'textinput',
        id: 'endpointName',
        label: 'Virtual MIDI endpoint name',
        width: 8,
        default: 'Companion Pro Tools HUI',
      },
      {
        type: 'number',
        id: 'releaseMs',
        label: 'HUI button release delay (ms)',
        width: 4,
        default: 20,
        min: 1,
        max: 200,
      },
    ]
  }

  updateActions() {
    UpdateActions(this)
  }

  updateFeedbacks() {
    UpdateFeedbacks(this)
  }

  updateVariableDefinitions() {
    UpdateVariables(this)
  }

  publishVariables() {
    const values = {
      endpoint_name: this.endpointName,
      protools_connected: this.proToolsConnected ? 1 : 0,
    }

    for (let i = 0; i < 8; i++) {
      values[`track_${i + 1}_muted`] = this.trackMute[i] == null ? -1 : this.trackMute[i] ? 1 : 0
    }

    this.setVariableValues(values)
  }

  startHelper() {
    this.helper.stop()
    this.trackMute.fill(null)
    this.proToolsConnected = false
    this.publishVariables()
    this.checkFeedbacks()

    if (process.platform !== 'win32') {
      this.updateStatus(InstanceStatus.BadConfig, 'Windows only prototype')
      return
    }

    try {
      this.updateStatus(InstanceStatus.Connecting, 'Starting Windows MIDI helper')
      this.helper.start(this.endpointName, Number(this.config.releaseMs || 20))
    } catch (error) {
      this.updateStatus(InstanceStatus.ConnectionFailure, error.message)
      this.log('error', error.stack || error.message)
    }
  }

  sendHelperCommand(payload) {
    try {
      this.helper.send(payload)
    } catch (error) {
      this.updateStatus(InstanceStatus.ConnectionFailure, error.message)
      this.log('error', error.message)
    }
  }

  handleHelperMessage(msg) {
    switch (msg.event) {
      case 'ready':
        this.updateStatus(InstanceStatus.Ok, `Virtual MIDI ready: ${msg.endpoint || this.endpointName}`)
        this.endpointName = msg.endpoint || this.endpointName
        this.publishVariables()
        break

      case 'connected':
        this.proToolsConnected = Boolean(msg.connected)
        this.publishVariables()
        this.checkFeedbacks('protools_connected')
        break

      case 'mute': {
        const index = Number(msg.track) - 1
        if (index >= 0 && index < 8) {
          this.trackMute[index] = Boolean(msg.muted)
          this.publishVariables()
          this.checkFeedbacks('track_muted')
        }
        break
      }

      case 'state':
        this.proToolsConnected = Boolean(msg.connected)
        if (Array.isArray(msg.mutes)) {
          for (let i = 0; i < Math.min(8, msg.mutes.length); i++) {
            this.trackMute[i] = msg.mutes[i] == null ? null : Boolean(msg.mutes[i])
          }
        }
        this.publishVariables()
        this.checkFeedbacks()
        break

      case 'warning':
        this.log('warn', msg.message || 'Windows MIDI helper warning')
        break

      case 'error':
        this.updateStatus(InstanceStatus.ConnectionFailure, msg.message || 'Windows MIDI helper error')
        this.log('error', msg.detail || msg.message || 'Windows MIDI helper error')
        break
    }
  }

  handleHelperError(error) {
    this.updateStatus(InstanceStatus.ConnectionFailure, error.message)
    this.log('error', error.stack || error.message)
  }

  handleHelperExit(code, signal) {
    this.proToolsConnected = false
    this.publishVariables()
    this.checkFeedbacks()
    if (code !== 0 && code != null) {
      this.updateStatus(InstanceStatus.ConnectionFailure, `Windows MIDI helper exited with code ${code}`)
    } else if (signal) {
      this.updateStatus(InstanceStatus.Disconnected, `Windows MIDI helper stopped (${signal})`)
    }
  }
}

runEntrypoint(ProToolsHuiVirtualInstance, UpgradeScripts)
