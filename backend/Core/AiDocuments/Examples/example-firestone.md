# Example: an idle game, Firestone

A real flow, built by hand in the editor, that keeps the idle game Firestone going on its own. The
real one handles thirteen kinds of notification; four are kept here, one for each way they are
built, and the other nine repeat these with other templates.

Read it for a flow in a game, or in any application that draws its own pixels: nothing to read with
OCR and no names from Windows, only pictures.

## What it shows

- **A forever loop over things that may not be there.** Each pass looks for every notification in
  turn. One that is not showing is normal, so its search has no Failure branch and the pass goes on.
- **Find, then Wait For.** Whether a notification is showing is already decided, so it is a `Find
  Image` that costs nothing when it is not. What a click opens takes a moment, so it is a `Wait For
  Image` with a short timeout.
- **Launch and resize inside the loop.** A launch of a window already open does nothing, so this
  starts the game again only if it closed, and the resize puts it back at the size its templates
  were cut at. The notifications are looked for under the resize's Success, so a window that is not
  at that size is never searched: the next pass tries again.
- **`Find All Images`** for every claim button on one screenshot - the Alchemist's - and a loop of
  `Wait For Image` where each click changes the screen, its experiments.
- **Loops inside loops, and a `Break` that leaves the outer one** when there is no slot left - the
  research.
- **A forever loop its own Failure branch ends** - the map's claims.
- **Points and drags** for a map that has to be moved to be seen.
- **Several templates in one search** where any of them will do, and **shape and brightness** where
  a greyed-out copy of the same picture must not match.
- **Scales with area**: a game stretches its contents with its window, so its templates grow and
  shrink with it.

## The flow

```sflw
Script Language: v1
Flow:    Firestone
Id:      5704d895-1fc9-495a-a24f-7028ec6b6125
Kind:    automation
Description:
  # Starts Firestone through Steam, then for ever works through the notifications down the left of
  # its window: opens each one that is showing, claims what it offers, starts what can be started,
  # and closes it.

Areas:
  <[ Firestone ]>  main   window process <[ Firestone ]>   started by <[ steam://rungameid/1013320 ]>   scales with area

Points:
  <[ Research list ]>  inside <[ Firestone ]>   ratio 0.21 0.10
  <[ Map centre end ]>  inside <[ Firestone ]>   ratio 0.66 0.19
  <[ Map centre start ]>  inside <[ Firestone ]>   ratio 0.31 0.16
  <[ Map reset end ]>  inside <[ Firestone ]>   ratio 0.21 0.89
  <[ Map reset start ]>  inside <[ Firestone ]>   ratio 0.94 0.11   at 120dpi
  <[ Map scroll down end ]>  inside <[ Firestone ]>   ratio 0.99 0.14
  <[ Map scroll down start ]>  inside <[ Firestone ]>   ratio 0.99 0.89

Templates:
  <[ template-01ofd.png ]>    click 32 33   captured 1753x909 at 120dpi
  <[ template-030tw.png ]>    click 31 32   captured 1753x909 at 120dpi
  <[ template-195el.png ]>    click 41 41   captured 1753x909 at 120dpi
  <[ template-1u5oz.png ]>    click 173 48   captured 1753x909 at 120dpi
  <[ template-32uuy.png ]>    click 129 51   captured 1753x909 at 120dpi
  <[ template-3fe6m.png ]>    click 224 46   captured 1753x909 at 120dpi
  <[ template-3vx5e.png ]>    click 31 32   captured 1753x909 at 120dpi
  <[ template-52k9t.png ]>    click 28 22   captured 1753x909 at 120dpi
  <[ template-64naw.png ]>    click 27 21   captured 1753x909 at 120dpi
  <[ template-76cv5.png ]>    click 54 53   captured 1753x909 at 120dpi
  <[ template-7d92d.png ]>    click 106 38   captured 1753x909 at 120dpi
  <[ template-7p7wt.png ]>    click 118 42   captured 1753x909 at 120dpi
  <[ template-8rbof.png ]>    click 27 20   captured 1753x909 at 120dpi
  <[ template-ad4sj.png ]>    click 109 39   captured 1753x909 at 120dpi
  <[ template-aw9n0.png ]>    click 125 47   captured 1753x909 at 120dpi
  <[ template-bki0n.png ]>    click 103 37   captured 1753x909 at 120dpi
  <[ template-ca8vy.png ]>    click 78 26   captured 1753x909 at 120dpi
  <[ template-d72cc.png ]>    click 345 22   captured 1753x909 at 120dpi
  <[ template-dphex.png ]>    click 34 34   captured 1753x909 at 120dpi
  <[ template-fu1tx.png ]>    click 75 32   captured 1753x909 at 120dpi
  <[ template-g7zz2.png ]>    click 31 32   captured 1753x909 at 120dpi
  <[ template-gu6ei.png ]>    click 33 33   captured 1753x909 at 120dpi
  <[ template-iyvk1.png ]>    click 146 61   captured 1753x909 at 120dpi
  <[ template-j852z.png ]>    click 31 32   captured 1753x909 at 120dpi
  <[ template-m3iqm.png ]>    click 33 34   captured 1753x909 at 120dpi
  <[ template-sy87l.png ]>    click 173 48   captured 1753x909 at 120dpi
  <[ template-unk19.png ]>    click 42 43   captured 1753x909 at 120dpi
  <[ template-vhbsl.png ]>    click 40 43   captured 1753x909 at 120dpi
  <[ template-wyl78.png ]>    click 345 22   captured 1753x909 at 120dpi
  <[ template-ytm77.png ]>    click 173 48   captured 1753x909 at 120dpi

Steps:

# An idle game keeps making notifications, so the flow keeps working through them. One that is not
# showing is normal: no search for a notification has a Failure branch, and the pass goes on.
Loop            <[ Work through the notifications ]>   forever
 # Inside the loop on purpose: a launch of a window already open does nothing, so this starts the
 # game again only if it closed.
 Launch          <[ Start Firestone ]>   opens <[ Firestone ]>
  Success:
   # Back to the size the templates were cut at, 1753 x 909 inside, so they match at their own size.
   Resize Window   <[ Firestone ]>   size 1771 956
    # Only a window at that size is looked through: one that would not resize is tried again on
    # the next pass.
    Success:
     # Arena of Kings only needs opening and closing for its notification to go.
     Find Image      <[ Find Arena of Kings ]>   template <[ template-dphex.png ]> accuracy 0.8   in <[ Firestone ]>
      Success:
       Move            to <[ Find Arena of Kings ]>
       Click
       Wait For Image  <[ Find Arena of Kings close ]>   template <[ template-3vx5e.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
        Success:
         Move            to <[ Find Arena of Kings close ]>
         Click
     # The Alchemist holds up to 3 rewards to claim and 3 experiments to start.
     Find Image      <[ Find Alchemist ]>   template <[ template-gu6ei.png ]> accuracy 0.8   in <[ Firestone ]>
      Success:
       Move            to <[ Find Alchemist ]>
       Click
       # Its window slides in.
       Wait            500ms
       # One screenshot and every claim button on it: the Success branch clicks each one in turn.
       Find All Images <[ Find every Alchemist claim ]>   template <[ template-ad4sj.png ]> accuracy 0.8   in <[ Firestone ]>
        Success:
         Move            to <[ Find every Alchemist claim ]>
         Click
         Wait            500ms
       # Starting an experiment changes the screen, so each one is looked for again rather than all
       # from one screenshot.
       Loop            <[ Start up to 3 experiments ]>   3 times
        # Any of the three templates will do.
        Wait For Image  <[ Find an experiment to start ]>   template <[ template-7d92d.png ]> accuracy 0.8  template <[ template-7p7wt.png ]> accuracy 0.8  template <[ template-bki0n.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 2000ms
         Success:
          Move            to <[ Find an experiment to start ]>
          Click
       Wait            500ms
       Wait For Image  <[ Find Alchemist close ]>   template <[ template-j852z.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
        Success:
         Move            to <[ Find Alchemist close ]>
         Click
     # Research: claim what has finished, then fill the free slots from the list.
     Find Image      <[ Find Research ]>   template <[ template-m3iqm.png ]> accuracy 0.8   in <[ Firestone ]>
      Success:
       Move            to <[ Find Research ]>
       Click
       # Two slots, so two claims at most.
       Loop            <[ Claim both research slots ]>   2 times
        Wait For Image  <[ Find research claim ]>   template <[ template-fu1tx.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
         Success:
          Move            to <[ Find research claim ]>
          Click
          Wait            250ms
        Wait            500ms
       # The list scrolls only with the cursor over it.
       Move            to point <[ Research list ]>
       Click
       # To the end of the list, to work back up through it.
       Scroll          down 50
       # The scroll animates.
       Wait            500ms
       # The list is two screens long, so two passes, each filling up to both slots.
       Loop            <[ Look through the research list ]>   2 times
        Loop            <[ Fill both research slots ]>   2 times
         # No free slot left: stop looking through the list altogether, not only this screen of it.
         Find Image      <[ Find a free research slot ]>   template <[ template-3fe6m.png ]> accuracy 0.8   in <[ Firestone ]>
          Failure:
           Break           out of <[ Look through the research list ]>
         # Shape and brightness at 0.95: a research that cannot be started is the same picture, greyed
         # out. Any of the templates will do.
         Find Image      <[ Find a research to start ]>   template <[ template-vhbsl.png ]> accuracy 0.95  template <[ template-195el.png ]> accuracy 0.95  template <[ template-unk19.png ]> accuracy 0.95   match shape and brightness   in <[ Firestone ]>
          Success:
           Move            to <[ Find a research to start ]>
           Click
           Wait For Image  <[ Find research start ]>   template <[ template-iyvk1.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
            Success:
             Move            to <[ Find research start ]>
             Click
             Wait            500ms
           Wait            500ms
        # Up one screen of the list.
        Scroll          up 28
       Wait For Image  <[ Find Research close ]>   template <[ template-g7zz2.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
        Success:
         Move            to <[ Find Research close ]>
         Click
     # The map: claim the missions that finished, then start new ones until the slots are full. They
     # sit anywhere on the map, so it is dragged to see all of it.
     Find Image      <[ Find Map ]>   template <[ template-01ofd.png ]> accuracy 0.8   in <[ Firestone ]>
      Success:
       Move            to <[ Find Map ]>
       Click
       Wait            500ms
       # Claims until there is nothing left to claim: a forever loop its own Failure branch ends.
       Loop            <[ Claim every finished mission ]>   forever
        Wait For Image  <[ Find a finished mission ]>   template <[ template-ca8vy.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
         Success:
          Move            to <[ Find a finished mission ]>
          Click
         # Nothing left to claim: close the reward window if one is open, and stop.
         Failure:
          Wait For Image  <[ Find the reward ok ]>   template <[ template-sy87l.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
           Success:
            Move            to <[ Find the reward ok ]>
            Click
          Wait            500ms
          Break           out of <[ Claim every finished mission ]>
       # To the position the map starts from, twice in case one drag does not reach it.
       Drag            at point <[ Map reset start ]> to point <[ Map reset end ]>
       Drag            at point <[ Map reset start ]> to point <[ Map reset end ]>
       # Then to the top centre, where the missions begin.
       Drag            at point <[ Map centre start ]> to point <[ Map centre end ]>
       # Down the map in three parts, starting what each part shows.
       Loop            <[ Start missions down the map ]>   3 times
        # No free slot: nothing more can be started.
        Find Image      <[ Find a free mission slot ]>   template <[ template-wyl78.png ]> accuracy 0.8   in <[ Firestone ]>
         Failure:
          Break           out of <[ Start missions down the map ]>
        # Any of the mission markers will do.
        Find All Images <[ Find every mission ]>   template <[ template-52k9t.png ]> accuracy 0.95  template <[ template-8rbof.png ]> accuracy 0.95  template <[ template-64naw.png ]> accuracy 0.95   match shape and brightness   in <[ Firestone ]>
         Success:
          # The mission started at the hit before may have taken the last slot.
          Find Image      <[ Find a free mission slot again ]>   template <[ template-d72cc.png ]> accuracy 0.8   in <[ Firestone ]>
           Failure:
            Break           out of <[ Start missions down the map ]>
          Move            to <[ Find every mission ]>
          Click
          # Its window offers one of three: ok on a finished mission, complete it early, or start it.
          Find Image      <[ Find mission ok ]>   template <[ template-ytm77.png ]> accuracy 0.8   in <[ Firestone ]>
           Success:
            Move            to <[ Find mission ok ]>
            Click
          Find Image      <[ Find complete early ]>   template <[ template-aw9n0.png ]> accuracy 0.8   in <[ Firestone ]>
           Success:
            Move            to <[ Find complete early ]>
            Click
            Find Image      <[ Find complete early ok ]>   template <[ template-1u5oz.png ]> accuracy 0.8   in <[ Firestone ]>
             Success:
              Move            to <[ Find complete early ok ]>
              Click
          Wait For Image  <[ Find mission start ]>   template <[ template-32uuy.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
           Success:
            Move            to <[ Find mission start ]>
            Click
          # Whatever it offered, its window is closed before the next hit.
          Find Image      <[ Find mission window close ]>   template <[ template-76cv5.png ]> accuracy 0.95   match shape and brightness   in <[ Firestone ]>
           Success:
            Move            to <[ Find mission window close ]>
            Click
          Wait            500ms
        # Down to the next part of the map.
        Drag            at point <[ Map scroll down start ]> to point <[ Map scroll down end ]>
       Wait For Image  <[ Find Map close ]>   template <[ template-030tw.png ]> accuracy 0.8   in <[ Firestone ]>   timeout 1000ms
        Success:
         Move            to <[ Find Map close ]>
         Click
```
