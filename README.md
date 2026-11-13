# Projekt 1 - Endless-Parcours

Unity-Kursprojekt

Alpen-Cup spielt vor einer Bergkulisse mit Holzplattformen, einem rollenden Fußball und einem Tor mit Netz.

Ein sichtbarer Fußballschuh stößt den Ball zu Beginn jedes Parcours an. Lenke den Ball in der Luft auf eine markierte Landefläche und spiele dich anschließend bis zum grünen Tor vor. Ab Level 2 stehen zwei Landeflächen zur Wahl. Ab Level 4 musst du beim Anstoß nach links oder rechts lenken. Ein Sturz beendet den Versuch. Der höchste Levelstand wird lokal gespeichert.

**Quellcode:** [GameEnginesProject1-Public](https://github.com/Lostzudemlovely/GameEnginesProject1-Public)

## Unity-Projekt öffnen

1. Repository klonen oder das Quellen-ZIP vollständig entpacken.
2. In Unity Hub **Add project from disk** wählen und den Ordner mit `Assets`, `Packages` und `ProjectSettings` hinzufügen.
3. Mit **Unity 6000.0.24f1** öffnen und den Import abwarten.
4. `Assets/Scenes/MainMenu.unity` doppelklicken und oben **Play** drücken.
5. Im Hauptmenü **Spielen** wählen. Direktes Öffnen von `Game.unity` beginnt unmittelbar einen Lauf.

Verwendet wird **URP 17.0.3**. Das Projekt verwendet Unity-eigene Systeme und keine zusätzlichen Gameplay-Frameworks.

## Steuerung
 **WASD / Pfeiltasten:** Kugel bewegen.
**Leertaste kurz / halten:** kleiner / voller Sprung; Absprung nur von einer begehbaren Fläche.
**Grünes Tor:** Level abschließen, automatisch nächste Strecke starten.
**Sturz unter Y = -5:** Game Over.
**R / Neuer Lauf:** Lauf bei Level 1, Score 0 und Zeit 0 beginnen.
**Esc:** Pause / Fortsetzen; Fokusverlust pausiert den laufenden Versuch ebenfalls.
**Hauptmenü:** aus Pause und Game Over erreichbar.
**Ton: An/Aus:** Musik und Effekte für die Sitzung stummschalten.
**Beenden:** Windows-Spiel schließen.

Jeder Lauf erhält einen neuen Startwert für die Streckengenerierung. In jedem Sechserblock kommen Zickzack, Sprunginseln, Balance-Stege, Slalom, Drehbalken und bewegliche Plattformen als Hauptstrecke vor. Ab Level 4 kann ein Parcours zwei dieser Abschnitte verbinden. Die Schwierigkeit steigt bis Level 11. Danach entstehen weitere Layouts mit denselben Obergrenzen. Für reproduzierbare Strecken lässt sich im Unity-Inspector beim LevelGenerator `Randomize Seed Each Run` ausschalten und ein eigener Seed einstellen.

## Projektstruktur

Die Szenen sind `MainMenu` und `Game`. Das Audio-Singleton bleibt beim Wechsel erhalten. Die Laufzeitskripte liegen unter `Assets/Scripts`. Musik und Soundeffekte wurden mit `Tools/generate_audio.py` synthetisiert. Der eigene HLSL-Shader für das Ziellicht liegt unter `Assets/Shaders/PulseGlow.shader`.
