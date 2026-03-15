---
name: Separate attributes onto own lines
description: User prefers each C# attribute on its own line rather than combined (e.g. [Theory] and [AutoData] on separate lines, not [Theory, AutoData])
type: feedback
---

Each C# attribute should be on its own line rather than combined.

**Why:** User's style preference for readability.

**How to apply:** Write `[Theory]\n[AutoData]` not `[Theory, AutoData]`. Applies to all attribute usage, not just test attributes.
