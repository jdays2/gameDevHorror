# Stalker adjacent attack frames

Separate dense attack family:1464 whole RGBA frames at224 pixels/unit on a576×576 world-registered canvas. Each48-phase entry/return bridge now contains15 samples; strike retains24 samples. The historical base792 family and canonical painter are unchanged.

Static art/packing and the subsequent actual runtime timing review passed separately. The installed config reserves0.24s for the return and uses linear sample indices. [The full Bridge15 replay](../../../../Unused/Visual/StalkerWholeFrameReview01/RuntimeDenseFinal01/review.md) covers13,860 samples across9 authored configurations, including an independent check of the full interrupted-attack ranges. Historical Bridge8 captures are not substituted for this evidence. The later shared eye-threshold fix and release validation are tracked in [the integration report](../../../../../Docs/Reports/StalkerWholeSprites05.md).

## Provenance and reproducibility

`compose_adjacent.py` freezes the actual C# export and all canonical materials/code hashes in `attack-composition.json`. Before rendering, it verifies every hip, knee and foot array of the792 retained poses: old bridge sample k equals new sample2k; all24 strike poses are unchanged. Raw PNG and all8 per-pose masks were copied byte-exact for those792 poses (7128 files); only672 odd bridge samples were rendered. `copied-approved-files.json` records every source/target/hash. No new material, anatomical attachment, source alpha matte, joint cap, ghost crossfade or runtime IK was introduced.

The native source remains the approved PaleFlesh Walk00/Attack03 and Locomotion02 anatomy/atlas, with source-ownership02 and one continuous inverse polar UV sample per whole leg. Canonical source hashes are embedded in the composition manifest. This family does not change author/license provenance or introduce a new generated drawing.

Geometry was checked separately for all1464 poses in `geometry-checks.json`: maximum independently measured chain error1.07e−7, positive projected bends, no foot below the authored floor and at least3 authored contacts. This is a coordinate/authoring check, not a claim about real physical3D volume or body support.

## Pixel and art review

`audit_adjacent.py` tested every one of the15 real limb-mask pairs for all672 new frames:10,080 comparisons. The11880 previous comparisons are inherited only through the proved byte identity of the retained792 frames. Body exclusion uses actual per-pose torso alpha; there is no hip-sphere exemption.

- 0 bounded holes in any new leg mask at alpha>4 or>128.
- 0 disconnected leg/whole masks at alpha>4;0 canvas-edge pixels.
- 0 nonpositive material Jacobians; minimum0.412245.
- 0 alpha<230 pixels in the four-pixel-eroded solid interior.
- 126 RF/RM pair contacts remain counted:906 pixels total, maximum21 pixels. Source UV is proximal tissue18–29 native pixels from the hip and at least56.57 pixels from the knee. The maximum contexts read as a natural Y attachment beside the torso, with no distal crossing or cut shaft.

`prove_adjacent_flags.py` traces all784 faint edge pixels outside a two-pixel solid dilation. Every alpha byte is reproduced by a single nonnegative bilinear sample.672 have unchanged native alpha neighbours; the112 source-owner-changed neighbours are all hidden by actual torso alpha. These counts remain in the evidence. No alpha erosion, threshold boost, hole fill or detached-pixel removal was applied.

All672 new frames were actually inspected on green in84 fixed native pages, each1304×978; tool emission used original detail without a resize notice. All page/raw hashes were rechecked after viewing. Six worst contact/AA cases were also inspected on green/dark/light at native1x and nearest4x in `QA/flag-contexts-00..02.png`. No new visible seam, root needle, chopped blade, canvas clipping, detached spur or mandatory volume correction was found.

The independent reviewer separately inspected all672 new frames on both dark/light backgrounds in168 native pages, rechecked every page/raw hash and independently proved792 old Raw copies. Their report is [AttackAdjacentDarkLight01](../../../../Unused/Visual/StalkerWholeFrameReview01/AttackAdjacentDarkLight01/review.md). Its scope is static artwork; runtime temporal acceptance remains open. Root also requested an additional cold material audit.

## Packing and handoff

`pack_adjacent.py` uses the canonical components/RGB-padding helpers without modifying them.1464 frames deduplicate to930 full RGBA images and8 PNG atlases under `Assets/EmergencyShift/Art/Enemies/Stalker/Postures05/AttackAdjacent01`. Packing preserves every Raw alpha byte and every alpha-positive RGBA byte, including alpha1–8. Padding changes hidden alpha-zero RGB only. No contour repair is performed.

`attack-packed-roundtrip.json` proves that every atlas rectangle/pivot reconstructs its576×576 canvas with all visible RGBA bytes exact. Raw→Clean alpha changes:0. Added coverage:0. The three-pixel atlas padding does not move the world origin288,288 or alter224PPU.

The `entryFraction=.15`, `returnFraction=.85` and completion`.97` fields retain the authored source recipe. They are not approval of the old runtime timing. Root owns the shared duration-aware playback timing, new-family installation, Unity import/meta settings and corrected runtime capture. No Unity scene, prefab, definition, C# renderer or Sleeper file was edited by this subtask. Итоговый контроль авторинга: неприменим.

## Frozen identifiers

| Evidence | SHA256 |
|---|---|
| Actual1464 pose export | `d6615dc8da453cb1407e37d581acab90d001e50987597161da6bd0423b7d42bf` |
| Composition manifest | `95d9f5b3efeada473a4c75eb81b7ce54da3a8c98ba0e97ca463416def713b519` |
| Sprite manifest | `e2ae885fa40cf8f6ac5b713afabdb15a796dc93c7f7d133053d2feff07f74dc2` |
| New672 Raw aggregate | `4508adace3ef2cd3e3c204b84bc5693e4a88ea08241b1f95ff6feffe596cefee` |
| New672 leg-mask aggregate | `050aa15f6a98baa2fe456d332064afa2720a3c4c27a7d87e0c225a505859e406` |
| Canonical painter | `2777a84fff7194b1be4aa75642f47801180703f60b6b6b1e5790dd53dc24dd7e` |

Numerical flags and source traces are retained in `QA`; actual green coverage is `ReviewNative/green-review.json`. These are completed evidence, not a competing task plan or an artistic approval on behalf of the owner.
