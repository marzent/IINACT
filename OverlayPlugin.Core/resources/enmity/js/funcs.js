'use strict'

let jobEnumToName = {
  0: 'UNKNOWN',
  1: 'GLA',
  2: 'PGL',
  3: 'MRD',
  4: 'LNC',
  5: 'ARC',
  6: 'CNJ',
  7: 'THM',
  8: 'CRP',
  9: 'BSM',
  10: 'ARM',
  11: 'GSM',
  12: 'LTW',
  13: 'WVR',
  14: 'ALC',
  15: 'CUL',
  16: 'MIN',
  17: 'BTN',
  18: 'FSH',
  19: 'PLD',
  20: 'MNK',
  21: 'WAR',
  22: 'DRG',
  23: 'BRD',
  24: 'WHM',
  25: 'BLM',
  26: 'ACN',
  27: 'SMN',
  28: 'SCH',
  29: 'ROG',
  30: 'NIN',
  31: 'MCH',
  32: 'DRK',
  33: 'AST',
  34: 'SAM',
  35: 'RDM',
  36: 'BLU',
  37: 'GNB',
  38: 'DNC',
};

let jobNameToRole = {
  PLD: 'TANK',
  WAR: 'TANK',
  GLD: 'TANK',
  MRD: 'TANK',
  DRK: 'TANK',
  GNB: 'TANK',

  CNJ: 'HEALER',
  WHM: 'HEALER',
  SCH: 'HEALER',
  AST: 'HEALER',

  PGL: 'DPS',
  LNC: 'DPS',
  ARC: 'DPS',
  THM: 'DPS',
  MNK: 'DPS',
  DRG: 'DPS',
  BRD: 'DPS',
  BLM: 'DPS',
  ACN: 'DPS',
  SMN: 'DPS',
  ROG: 'DPS',
  NIN: 'DPS',
  MCH: 'DPS',
  SAM: 'DPS',
  RDM: 'DPS',
  BLU: 'DPS',
  DNC: 'DPS',
};

let isPet = (entity) => {
  return entity.OwnerID != 0;
};

Vue.filter('jobrole', function(entity) {
  if (!entity)
    return 'UNKNOWN';
  if (isPet(entity))
    return 'Pet';
  if (entity.isMe)
    return 'YOU';
  let jobName = jobEnumToName[entity.Job];
  let role = jobNameToRole[jobName];
  if (role != null)
    return role;
  return 'UNKNOWN';
});

Vue.filter('jobname', function(entity) {
  if (!entity)
    return 'UNKNOWN';
  if (isPet(entity))
    return 'Pet';
  if (entity.isMe)
    return 'YOU';
  let jobName = jobEnumToName[entity.Job];
  if (jobName != null)
    return jobName;
  return 'UNKNOWN';
});

let hpPercentString = (entity) => {
  if (!entity)
    return '--';
  if (entity.MaxHP <= 0)
    return '0.00';
  return (100.0 * entity.CurrentHP / entity.MaxHP).toFixed(2);
};

Vue.filter('hpcolor', function(entity) {
  let percent = 100.0 * entity.CurrentHP / entity.MaxHP;
  if (percent > 75) return 'green';
  if (percent > 50) return 'yellow';
  if (percent > 25) return 'orange';
  return 'red';
});

Vue.filter('hppercent', function(entity) {
  return hpPercentString(entity);
});

Vue.filter('hatecolor', function(entity) {
  if (entity.HateRate == 100) return 'red';
  if (entity.HateRate > 75) return 'orange';
  if (entity.HateRate > 50) return 'yellow';
  return 'green';
});

Vue.filter('you', function(entity) {
  return entity.isMe ? 'YOU' : entity.Name;
});

Vue.filter('round', (x) => Math.round(x));

function formatNum(num) {
  return num.toString().replace(/(\d)(?=(\d{3})+$)/g, '$1,');
}

Vue.filter('numformat', (num) => {
  if (num == 0)
    return '--';
  return formatNum(num);
});

Vue.filter('relnumformat', (num) => {
  if (num == 0)
    return '--';
  return (num > 0 ? '+' : '') + formatNum(num);
});
