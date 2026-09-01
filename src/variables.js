module.exports = function updateVariables(self) {
  const definitions = [
    { variableId: 'endpoint_name', name: 'Virtual MIDI endpoint name' },
    { variableId: 'protools_connected', name: 'Pro Tools HUI keepalive active (1/0)' },
  ]

  for (let track = 1; track <= 8; track++) {
    definitions.push({
      variableId: `track_${track}_muted`,
      name: `HUI strip ${track} muted (-1 unknown, 0 off, 1 on)`,
    })
  }

  self.setVariableDefinitions(definitions)
  self.publishVariables()
}
