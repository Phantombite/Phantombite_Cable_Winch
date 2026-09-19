# DEV History — PhantomBite Cable Winch

## 2026-09-19 — Bereinigung
- Der Mod meldete sich im Log als `Phantombite_CableWinch`, der Core kennt `Phantombite_Cable_Winch`.
  Dadurch gingen Debug-Meldungen (Level 1/2) verloren. Name korrigiert; der Core normalisiert Namen jetzt
  zusätzlich selbst.
- Kompiliert fehlerfrei. Die Block-Logik wurde gelesen, aber nicht verändert.
- Offen: `TryGetDummies` setzt `PulleyWire_1`/`_2` als feste Namen voraus, weicht ein Dummy-Name ab, entsteht
  jedes Frame eine Ausnahme.

## 2026-03-22 — v1.0.0 — Initialer Release

- Pulley System aus NimbusMod extrahiert und als eigenständiger Mod veröffentlicht
- Umbenennung: Pulley → CableWinch in allen Dateinamen, SubtypeIds und Icons
- Audio umbenannt: Pulley_Sound → CableWinch_Sound
- Steam Workshop ID: 3689668160
- GitHub Repository: https://github.com/Phantombite/PhantomBiteCableWinch
- MIT License

### Hintergrund
Das Pulley-System war ursprünglich im NimbusMod enthalten. Es basiert auf dem ExtendedPistonBase System von SE und simuliert eine Seilwinde. Das Modell stammt aus einem fremden Mod und wurde übernommen. Das Seil-Visual-System ist noch nicht implementiert.
