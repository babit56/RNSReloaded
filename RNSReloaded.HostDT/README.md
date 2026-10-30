# HostDT

Various Reloaded mods rewritten to use the same patterns as workshop mods, so only host needs to have reloaded and this mod. Values are controlled in lobby using the chat, see `!help`.

Does not limit online play, but forces lobby and such to be marked as modded. Lobby should be online (friends only or online) so chat can be used.

Thanks to Glass for the original version of HostDT, Microwave for SoloTogether and Mino for R&S :3

## Notes
To have the right context (self & other), we piggyback of `bp_nothing` which we can run via `scr_enemy_add_pattern`. Values (zoom, gamespeed, etc) are applied at the start of the round (which we detect via `scr_battlecontroller_start_round`). For bullet hell and chaos, `bp_nothing` keeps running for the rest of the fight automatically

To work as expected when these mechs are used in vanilla, there's a couple special cases we have to handle. Values here are applied using the offending function as self/other:
- Merran's "lock in mech"(?), who saves old zoom and slows the game with an inline function
- Zooming, which always eventually uses `scrbp_zoom`
- Gamespeed, where we hook `setgamespeed(_temp)` to remodify speed afterwards. `scrbp_gamespeed` is not relevant since it gets inlined everywhere

## TODO
- [x] Merran breaks on faceoff since clients dont get speedup
    - [ ] Merran speed broken and not correlated with ping
- [ ] Finish extra mech mode?
- [x] make afk message pop up for everyone
- [x] hook into scr_lang_string("chatsys_client_joined") then print afk message afterwards to support langs
  - Or all the places that send client joined messages (there's like 3)
- integrate more other mods
    - [x] solotogether
        - [ ] permadeath
    - [ ] fullmetal?
    - [ ] steelheart
- [x] figure out how to run stuff periodically for chaos/bullet hell
- [x] Pattern dies at spell mirror, asha gem, mell immediately
- [x] !pingall
- [x] disable mod when not-host
    - (assuming in lobby) global.clientOwnId is 0 when host, >0 otherwise. Outside of lobby it might be whatever it was last, or 0, or -1...?
    - scrc_h_adventure_set_data and scr_lobbyclient_clear_mods to resp. disable and enable?
    - read clientOwnId every frame, toggle hooks when it changes
- [x] avoid double restart
- [x] fix mp -> sp map with special diff cases
- [x] Don't print out default settings
- [ ] spell mirror 3 avoid necesssary dmg
- [ ] !pingall doesn't show everyone in very large lobbies (e.g. 9 active clients)
- Testing
    - merran slowdown mech (stacking zoom, chaos zoom, client speedup)
    - st enrage fix in multi over net
    - only host can do commands
    - restart-less dest and diff change
