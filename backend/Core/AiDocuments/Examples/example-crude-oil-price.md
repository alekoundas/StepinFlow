# Example: a website, the crude oil price

A real flow, recorded on a website and then fixed by hand: it opens the Trading Economics crude oil
page in Chrome and sends a notification with the price. Small enough to read whole, and it shows the
most common fix a recording needs.

Read it for a flow on a website or in any application whose text can be read: a browser tab found by
its address, a box inside it read with OCR, and what was read sent on.

## What it shows

- **A website as the main area**: a browser tab found by its address. Launch brings that tab to the
  front, or opens the browser at the address when no tab has it.
- **An area inside the page**, measured in pixels from its corner, and a resize that puts the page
  back at the size it was measured at, so the area still sits on the price.
- **A value that changes is never a condition.** It is read, and what was read is used.
- **`Notify ... with result of`** puts what the check read at the top of the message.
- **Every Failure branch that matters ends the execution with a reason**, so an execution that did
  nothing never reads as one that worked.

## As it was recorded

The recorder's plain draft. The tester marked the price as a check, so the draft waits for the words
read there - the price that day.

```sflw recorded
Script Language: v1
Flow:    Price - Crude Oil
Id:      6d2a42bc-4740-4773-91e4-72cc0d4d0e2b
Kind:    automation
Description:
  # Find or open a browser with the trading economics website and send a notification with the crude oil price

Areas:
  <[ tradingeconomics.com/commodity/crude-oil ]>  main   window process <[ chrome ]>   tab <[ https://tradingeconomics.com/commodity/crude-oil ]>   started by <[ "C:\Program Files\Google\Chrome\Application\chrome.exe" ]>
  <[ Check 104.12 area ]>  inside <[ tradingeconomics.com/commodity/crude-oil ]>   offset 1463 372  size 81 78   at 120dpi

Steps:

Launch          <[ Launch tradingeconomics.com/commodity/crude-oil ]>   opens <[ tradingeconomics.com/commodity/crude-oil ]>
Resize Window   <[ tradingeconomics.com/commodity/crude-oil ]>   size 1950 2100
# recorded after a 0.0s wait
Wait For Text   <[ Check 104.12 ]>   contains <[ 104.12 ]>   in <[ Check 104.12 area ]>   timeout 10000ms
 Failure:
  End Execution   failed  <[ Check 104.12 found nothing within 10.0s ]>
```

## What was wrong with it

- **It waited for that day's price.** `contains <[ 104.12 ]>` passes only while the price is 104.12,
  so the first execution after it moved failed, saying the price was not found. A value that
  changes from one execution to the next is read, never checked for: `matches <[ (\d+\.\d+) ]>`
  takes whatever number is in the box.
- **It read the price and did nothing with it.** The point of the flow was to send it, so a
  `Notify` under the check sends what it read.
- **The names were the recording's.** `Check 104.12` and `Check 104.12 area` say what was on the
  screen once; `Read the price` and `Price box` say what they are for.
- **A failed launch or resize carried on.** With nothing in their Failure branches, a browser that
  never opened, or a page left at another size, went on to read a box that was not on the price.
  What needs the page goes under the Success of the step that readied it, and each Failure ends
  the execution saying what went wrong.

## As it was fixed

```sflw
Script Language: v1
Flow:    Price - Crude Oil
Id:      6d2a42bc-4740-4773-91e4-72cc0d4d0e2b
Kind:    automation
Description:
  # Finds or opens the Trading Economics crude oil page in Chrome and sends a notification with
  # the price.

Areas:
  <[ Crude oil page ]>  main   window process <[ chrome ]>   tab <[ https://tradingeconomics.com/commodity/crude-oil ]>   started by <[ "C:\Program Files\Google\Chrome\Application\chrome.exe" ]>
  <[ Price box ]>  inside <[ Crude oil page ]>   offset 1463 372  size 81 78   at 120dpi

Steps:

# Brings the tab at that address to the front, or opens Chrome there when no tab has it.
Launch          <[ Open the crude oil page ]>   opens <[ Crude oil page ]>
 Success:
  # The price box is measured in pixels from the page's corner, so the page goes back to the size
  # it was measured at.
  Resize Window   <[ Crude oil page ]>   size 1950 2100
   Success:
    # The price changes from one execution to the next, so this takes whatever number is in the
    # box, never the one seen while recording.
    Wait For Text   <[ Read the price ]>   matches <[ (\d+\.\d+) ]>   in <[ Price box ]>   timeout 10000ms
     Success:
      Notify          <[ Crude oil price ]>   with result of <[ Read the price ]>
     Failure:
      End Execution   failed  <[ no crude oil price was shown within 10 seconds ]>
   Failure:
    End Execution   failed  <[ Chrome would not resize, so the price box would not sit on the price ]>
 Failure:
  End Execution   failed  <[ Chrome did not open the crude oil page ]>
```
