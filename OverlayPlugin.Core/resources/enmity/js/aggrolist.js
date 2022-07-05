'use strict'

let aggrolist = new Vue({
  el: '#aggrolist',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    combatants: null,
    hide: false,
  },
  attached: function() {
    window.addOverlayListener('EnmityAggroList', this.update);
    document.addEventListener('onOverlayStateUpdate', this.updateState);
    document.addEventListener('onExampleShowcase', this.showExample);
    window.startOverlayEvents();
  },
  detached: function() {
    window.removeOverlayListener('EnmityAggroList', this.update);
    document.removeEventListener('onOverlayStateUpdate', this.updateState);
  },
  methods: {
    update: function(enmity) {
      this.updated = true;
      this.combatants = enmity.AggroList || [];

      // Sort by aggro, descending.
      this.combatants.sort((a, b) => b.HateRate - a.HateRate);
    },
    updateState: function(e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function() {
      this.collapsed = !this.collapsed;
    },
    showExample: function() {
      this.update({
        AggroList: [
          {
            isMe: false,
            isCurrentTarget: true,
            HateRate: 90,
            Name: 'Tank',
            Job: 'PLD',
            MaxHP: 100,
            CurrentHP: 3
          },
          {
            isMe: false,
            isCurrentTarget: false,
            HateRate: 3,
            Name: 'Off-Tank',
            Job: 'WAR',
            MaxHP: 5000,
            CurrentHP: 4980,
          }
        ]
      });
    }
  },
});
