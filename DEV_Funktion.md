# DEV Funktion — PhantomBite Cable Winch

## Zweck
Seilwinden-Blöcke für Space Engineers in drei Größen. Basiert auf dem ExtendedPistonBase System — verhält sich wie ein sehr langer Kolben mit angepassten Parametern.

## Blöcke

| Block | Grid | Größe | Reichweite | SubtypeId |
|-------|------|-------|------------|-----------|
| Large Cable Winch Base | Large | 1x1x1 | 1000m | CableWinch_Base |
| Large Cable Winch Top | Large | 1x1x1 | — | CableWinch_Top |
| Small Cable Winch Base | Small | 3x3x3 | 200m | SG_CableWinch_Base |
| Small Cable Winch Top | Small | 3x3x3 | — | SG_CableWinch_Top |
| TINY Cable Winch Base | Small | 1x3x1 | 50m | CableWinch_Base_TINY |
| TINY Cable Winch Top | Small | 1x1x1 | — | CableWinch_Top_TINY |

## Technische Werte

| Parameter | Wert |
|-----------|------|
| MaxVelocity | 10 m/s |
| RequiredPowerInput | 0.002 MW |
| DefaultMaxImpulseAxis | 500.000 |
| DefaultMaxImpulseNonAxis | 500.000 |
| DangerousImpulseThreshold | 1.000.000 |
| MaxImpulse | Float.Max |
| SafetyDetach | 5 |

## Audio
- **Sound:** CableWinch_Sound
- **Dateien:** Audio/CableWinch_START.xwm, CableWinch_Run4.xwm, CableWinch_STOP.xwm
- **Loopable:** true, MaxDistance 15m, Volume 0.3

## GUI Kategorie
Aktuell noch unter `AtE Systems` — sollte auf `PhantomBite` umbenannt werden.

## Modelle
```
Models/Cubes/large/
├── CableWinch_Base.mwm + BS1
├── CableWinch_Top.mwm + BS1
├── CableWinch_Part1/2/3.mwm + BS1      (Seilsegmente — noch nicht genutzt)
├── CableWinch_Cable.mwm                 (Seil — noch nicht genutzt)
├── Cable_spin.mwm                       (Kabelrolle — noch nicht genutzt)
└── LG_Varianten aller obigen            (Large Grid Varianten)

Models/Cubes/small/
├── SG_CableWinch_Base.mwm + BS1
├── CableWinch_Base_TINY.mwm + BS1
├── CableWinch_Top_TINY.mwm + BS1
└── Weitere SG/TINY Varianten
```

## Dateistruktur
```
Phantombite_Cable_Winch/
├── modinfo.sbmi
├── metadata.mod
├── Audio/
│   ├── CableWinch_START.wav/.xwm
│   ├── CableWinch_Run4.wav/.xwm
│   └── CableWinch_STOP.wav/.xwm
├── Data/Cubeblocks/Cable Winch.sbc
├── Models/Cubes/large/ + small/
└── Textures/GUI/Icons/Cubes/
    ├── CableWinch_Base.dds
    ├── CableWinch_Base_TINY.dds
    ├── CableWinch_Top.dds
    └── CableWinch_Top_TINY.dds
```
