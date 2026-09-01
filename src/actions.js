const trackChoices = Array.from({ length: 8 }, (_, index) => ({
  id: index + 1,
  label: `HUI strip ${index + 1}`,
}))

module.exports = function updateActions(self) {
  self.setActionDefinitions({
    toggle_mute: {
      name: 'Toggle track mute',
      options: [
        {
          type: 'dropdown',
          id: 'track',
          label: 'Track / HUI strip',
          default: 1,
          choices: trackChoices,
        },
      ],
      callback: async (event) => {
        self.sendHelperCommand({ cmd: 'toggleMute', track: Number(event.options.track) })
      },
    },

    set_mute: {
      name: 'Set track mute',
      description: 'Requires a known mute LED state from Pro Tools. If the state is not known yet, no toggle is sent.',
      options: [
        {
          type: 'dropdown',
          id: 'track',
          label: 'Track / HUI strip',
          default: 1,
          choices: trackChoices,
        },
        {
          type: 'dropdown',
          id: 'muted',
          label: 'Mute',
          default: true,
          choices: [
            { id: true, label: 'On' },
            { id: false, label: 'Off' },
          ],
        },
      ],
      callback: async (event) => {
        self.sendHelperCommand({
          cmd: 'setMute',
          track: Number(event.options.track),
          muted: event.options.muted === true || event.options.muted === 'true',
        })
      },
    },

    request_state: {
      name: 'Request helper state',
      options: [],
      callback: async () => {
        self.sendHelperCommand({ cmd: 'getState' })
      },
    },
  })
}
