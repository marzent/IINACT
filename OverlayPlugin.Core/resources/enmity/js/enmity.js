'use strict';

let noTarget = {
  Name: '- none -',
  MaxHP: '--',
  CurrentHP: '--',
  Distance: '--',
  TimeToDeath: '',
};

let noEntry = {
  Enmity: 0,
  RelativeEnmity: 0,
};

let localeStrings = {
  'English': {
    target: 'Target',
    distance: 'Distance',
  },
  'French': {
    target: 'Cible',
    distance: 'Distance',
  },
  'Japanese': {
    target: 'ターゲット',
    distance: '距離',
  },
};

let enmity = new Vue({
  el: '#enmity',
  data: {
    updated: false,
    locked: false,
    collapsed: false,
    target: null,
    entries: null,
    myEntry: null,
    hide: false,
    strings: {},
  },
  attached: function() {
    window.callOverlayHandler({ call: 'getLanguage' }).then((msg) => {
      if (msg.language in localeStrings)
        this.strings = localeStrings[msg.language];
      else
        this.strings = localeStrings['English'];
      this.language = msg.language;

      window.addOverlayListener('EnmityTargetData', this.update);
      document.addEventListener('onExampleShowcase', this.showExample);
      document.addEventListener('onOverlayStateUpdate', this.updateState);
      window.startOverlayEvents();
    });
  },
  detached: function() {
    window.removeOverlayListener('EnmityTargetData', this.update);
    document.removeEventListener('onExampleShowcase', this.showExample);
    document.removeEventListener('onOverlayStateUpdate', this.updateState);
  },
  methods: {
    showExample: function() {
      this.update({
        Entries: [
          {
            isMe: false,
            isCurrentTarget: true,
            Enmity: 90,
            Name: 'Tank',
            Job: 'PLD',
            MaxHP: 100,
            CurrentHP: 3,
          },
          {
            isMe: false,
            isCurrentTarget: false,
            Enmity: 293,
            Name: 'Off-Tank',
            Job: 'WAR',
            MaxHP: 5000,
            CurrentHP: 4980,
          },
          {
            isMe: true,
            isCurrentTarget: false,
            Enmity: 5293,
            Name: 'Player',
            Job: 'BLM',
            MaxHP: 2000,
            CurrentHP: 2000,
          },
        ],
        Target: {
          Type: 2,
          ID: 1582,
          isCurrentTarget: true,
          Name: 'Mob',
          CurrentHP: 45300,
          MaxHP: 50000,
          Distance: 1,
        },
      });
    },
    update: function(enmity) {
      let player = updateRelativeEnmity(enmity);
      this.myEntry = player === null ? noEntry : player;

      if (enmity.Target)
        this.processTarget(enmity.Target);

      this.updated = true;
      this.entries = enmity.Entries;
      this.target = enmity.Target ? enmity.Target : noTarget;
      if (this.hide)
        document.getElementById('enmity').style.visibility = 'hidden';
      else
        document.getElementById('enmity').style.visibility = 'visible';
    },
    updateState: function(e) {
      this.locked = e.detail.isLocked;
    },
    toggleCollapse: function() {
      this.collapsed = !this.collapsed;
    },
    processTarget: function(target) {
      if (!this.targetHistory)
        this.targetHistory = new TargetHistory();

      this.targetHistory.processTarget(target);
      let secondsRemaining = this.targetHistory.secondsUntilDeath(target);
      if (secondsRemaining === null)
        target.TimeToDeath = '';
      else
        target.TimeToDeath = toTimeString(secondsRemaining, this.language);
    },
  },
});
