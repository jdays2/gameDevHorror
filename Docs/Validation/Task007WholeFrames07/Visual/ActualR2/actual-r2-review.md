# WholeFrames07 R2 — actual-alpha check

**Improved, but strict exterior alpha still FAILS.** Bone gate remains independent; no muscle/animation/owner approval.

Source SHA256 8ea1d69fe24d8d11219a9cd6f197a23d9d7893f0bb77f795e97b331b3ac73f1b;1254x1254RGBA. Both complete native darkRGB(24,26,30) and lightRGB(232,232,226) composites actually viewed. No visible opaque diagnostic-colored line or detached solid fragment found.

R1 toR2: detached components3150 to289; detached pixels30606 to1499 (95.10% fewer); maximum detached alpha2 to1. Nonzero pixels beyond the two-pixel128-core ring42900 to7576. The background is still not strictly alpha0. This is faint alpha1 residue, not a proved large skin wound.

Largest detached native top-left bounding boxes (exclusive right/bottom):

- [907, 527, 938, 561]: 542 pixels, alpha1.
- [887, 532, 902, 558]: 128 pixels, alpha1.
- [905, 437, 923, 450]: 121 pixels, alpha1.

All six recorded provenance hashes and both decoded alpha composites match. Actual viewed PNG hashes, full component ledger and metadata are in actual-r2-review.json. Source/alpha untouched; writes only in Visual/ActualR2.
