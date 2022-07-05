'use strict';

function toTimeString(floatSeconds, language) {
  let intSeconds = Math.floor(floatSeconds);
  let minutes = Math.floor(intSeconds / 60);
  let seconds = intSeconds % 60;
  let str = '';

  // TODO: handle other languages here!
  if (minutes > 0)
    str = minutes + 'm';

  str += seconds + 's';
  return str;
}

// Updates each enmity entry with a |RelativeEnmity| field.
// Returns the player's enmity for this set of entries, or null if not found.
// |enmity| is the entire object returned from the EnmityTargetData event.
function updateRelativeEnmity(enmity) {
  if (enmity.Entries === null)
    enmity.Entries = [];

  // Entries sorted by enmity, and keys are integers.
  // If only one, show absolute value (otherwise confusingly 0 for !isMe).
  let max = 0;
  if (Object.keys(enmity.Entries).length > 1)
    max = enmity.Entries[0].isMe ? enmity.Entries[1].Enmity : enmity.Entries[0].Enmity;

  let playerEnmity = null;
  for (let i = 0; i < enmity.Entries.length; ++i) {
    let e = enmity.Entries[i];
    e.RelativeEnmity = e.Enmity - max;
    if (e.isMe)
      playerEnmity = e;
  }
  return playerEnmity;
}

// Records hp values of a target over a time period in order to get estimates
// about time to death for that target.
class TargetHistory {
  constructor () {
    // Throw away entries older than this.
    this.keepHistoryMs = 30 * 1000;
    // Sample period between recorded entries.
    this.samplePeriodMs = 60;

    // Target ID => { hist: [integer hp values], lastUpdated: date }
    this.targetHistory = {};
  }

  // Stores info about target; returns seconds until death, null if unknown.
  // |target| is a top-level Target object from the EnmityTargetData event.
  // Should be called once per target per update.
  processTarget(target) {
    let now = +new Date();

    if (!this.targetHistory[target.ID]) {
      this.targetHistory[target.ID] = {
        hist: [],
        lastUpdated: now,
      };
    }

    let h = this.targetHistory[target.ID];
    if (now - h.lastUpdated > this.samplePeriodMs) {
      h.lastUpdated = now;
      // Don't update if hp is unchanged to keep estimate more stable.
      if (h.hist.length == 0 || h.hist[h.hist.length - 1].hp != target.CurrentHP)
        h.hist.push({ time: now, hp: target.CurrentHP });
    }

    // Toss old history events.
    while (h.hist.length > 0 && now - h.hist[0].time > this.keepHistoryMs)
      h.hist.shift();
  }

  // Returns seconds until death, null if unknown.
  // |target| is a top-level Target object from the EnmityTargetData event.
  secondsUntilDeath(target) {
    let h = this.targetHistory[target.ID];
    if (h.hist.length < 2)
      return null;

    let first = h.hist[0];
    let last = h.hist[h.hist.length - 1];
    let totalSeconds = (last.time - first.time) / 1000;

    // Some enemies regain hp, ignore these.
    if (first.hp <= last.hp || totalSeconds == 0)
      return null;

    let dps = (first.hp - last.hp) / totalSeconds;
    return last.hp / dps;
  }
}

class ExampleTargetHistory extends TargetHistory {
  secondsUntilDeath(target) {
    return 23.7;
  }
}