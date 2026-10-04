# SAM_UI Part O PR1 progress

Base: `sow/2026-Q3`. PR1 merged as SAM-BIM/SAM_UI#188 at `bea1256e02b944e84882647f9c1955ce91c765bb` on 2026-10-04. Local base updated.

## Completed
Matrix shows cooling control room and allows one cooled dwelling's room to be explicitly confirmed. Legacy missing room appears as attention and blocks Build. Systems call copies selected room from SAM record into guidance settings.

## Files changed
PartOMixedDesignSession, PartOMixedDwellingRow, PartOMixedDesignWindow, PartOMixedSystemsCall, cooling/design fixture/large project tests; this progress file.

## Validation
Mixed design suite: 113 passed, including legacy confirmation and 5,000-space case; focused cooling tests: 18 passed; PR Windows build and SPDX passed.

## Next step
No unresolved PR1 issues. Stop after PR1; do not start PR2 without a new request.
