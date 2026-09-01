const trackChoices = Array.from({ length: 8 }, (_, index) => ({
  id: index + 1,
  label: `HUI strip ${index + 1}`,
}))

module.exports = function updateFeedbacks(self) {
  self.setFeedbackDefinitions({
    track_muted: {
      name: 'Track is muted',
      type: 'boolean',
      label: 'Pro Tools HUI mute state',
      defaultStyle: {
        bgcolor: 0xff0000,
        color: 0xffffff,
      },
      options: [
        {
          type: 'dropdown',
          id: 'track',
          label: 'Track / HUI strip',
          default: 1,
          choices: trackChoices,
        },
      ],
      callback: (feedback) => {
        const index = Number(feedback.options.track) - 1
        return self.trackMute[index] === true
      },
    },

    protools_connected: {
      name: 'Pro Tools HUI keepalive is active',
      type: 'boolean',
      label: 'Pro Tools connected',
      defaultStyle: {
        bgcolor: 0x00aa00,
        color: 0xffffff,
      },
      options: [],
      callback: () => self.proToolsConnected === true,
    },
  })
}
