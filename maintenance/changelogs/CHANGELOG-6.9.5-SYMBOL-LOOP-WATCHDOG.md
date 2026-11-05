# LocalGPT 6.9.5 — repetition watchdog also catches symbol-only loops

## Reported (6.9.4 run)

During *Creature introduction*, `llama3.2:3b` answered `FORM: +---#---` and then streamed an endless grid of `-+---+---+---+…` lines. The tournament could not start because that participant never finished, and the repetition watchdog never fired.

## Cause

`ProviderStreamRepetitionWatchdog` only analyzed word tokens (runs of letters, digits and `_`). A grid made of `+`, `-`, `|`, `#`, `=` or box-drawing characters contains no word tokens at all, so the analysis always stopped at "fewer than `MinimumAnalyzedTokens` tokens". This was the case before 6.9.4 as well (Creature introduction was one of the three phases that already forced the guard on).

## Change

- New second pass `TryFindRepeatedSymbolCycle`, used when the word-token pass finds nothing:
  - looks at the newest 4096 characters only;
  - every non-whitespace symbol is its own token (letter/digit runs stay one token), whitespace is ignored so line breaks inside a grid do not hide the period;
  - fires only when the **whole** 4096-character window agrees with one period at the configured agreement (97 % short / 98.5 % long periods). The existing sampling rules still apply (initial observation, 4 consecutive suspicious samples, at least 6 s).
- Nothing else changed: the existing word-token pass, thresholds and runtime-policy values are unchanged, and so is the 6.9.4 salvage (`TrimRepeatedTail` keeps the text before the loop plus one copy and the step continues).

## Version

`6.9.4` → `6.9.5`.
