'use strict'

let targets = ['Target', 'Focus', 'Hover', 'TargetOfTarget'];

let localeStrings = {
  'English': {
    target: 'Target',
    distance: 'Distance',
    titles: {
      Target: 'Target',
      Focus: 'Focus',
      Hover: 'Hover',
      TargetOfTarget: 'ToT',
    },
  },
  'French': {
    target: 'Cible',
    distance: 'Distance',
    titles: {
      Target: 'Cible',
      Focus: 'Focus',
      Hover: 'Survol',
      TargetOfTarget: 'ToT',
    },
  },
  'Japanese': {
    target: 'ターゲット',
    distance: '距離',
    titles: {
      Target: 'ターゲット',
      Focus: 'フォーカス',
      Hover: 'ホバー',
      TargetOfTarget: 'TT',
    },
  },
};

let targetinfo = new Vue({
  el: '#targetinfo',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    targets: [],
    strings: {},
  },
  attached: function() {
    window.callOverlayHandler({ call: 'getLanguage' }).then((msg) => {
      if (msg.language in localeStrings)
        this.strings = localeStrings[msg.language];
      else
        this.strings = localeStrings['English'];

      window.addOverlayListener('EnmityTargetData', this.update);
      document.addEventListener('onOverlayStateUpdate', this.updateState);
      window.startOverlayEvents();
    });
  },
  detached: function() {
    window.addOverlayListener('EnmityTargetData', this.update);
    document.removeEventListener('onOverlayStateUpdate', this.updateState);
  },
  methods: {
    update: function(enmity) {
      this.updated = true;
      this.targets = [];
      for (let k of targets) {
        let t = enmity[k];
        if (t == null) {
          t = {
            Name: 'none',
            MaxHP: 0,
            CurrentHP: 0,
            Distance: 0,
          };
        }
        t.Key = this.strings.titles[k];
        this.targets.push(t);
      }
    },
    updateState: function(e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function() {
      this.collapsed = !this.collapsed;
    },
  },
});
