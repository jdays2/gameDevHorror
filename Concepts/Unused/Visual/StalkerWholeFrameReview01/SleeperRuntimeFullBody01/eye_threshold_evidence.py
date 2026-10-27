"""Preserve actual native neighbours and observed iris/pupil state before the threshold fix."""
import json

from PIL import Image, ImageDraw, ImageFont

from prepare_review import HERE, ROOT, SOURCE, digest, save


def main():
    profile = SOURCE / 'MechanicsSandbox-sleeper-live-ce174f83'
    manifest = json.loads((profile / 'manifest.json').read_bytes())
    facts = {row['frame']: row for row in manifest['frames']}
    animator = ROOT / 'Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs'
    code = animator.read_text(encoding='utf-8')
    assert ('bool open = !dead && presentation.Closure < 0.166f;' in code
            or 'bool open = !dead && eyeFrame == 0;' in code)
    assert 'public bool EyeOpen => pupil != null && pupil.enabled;' in code
    capture = ROOT / 'AgentScripts/CaptureCreatureScenarios05.cs'
    capture_code = capture.read_text(encoding='utf-8')
    assert 'eyeOpen = animator.EyeOpen' in capture_code
    selected = [[89, 90, 91, 92], [956, 957, 958, 959]]
    crop = (224, 224, 416, 416)
    page = Image.new('RGB', (768, 464), (22, 24, 28))
    draw = ImageDraw.Draw(page)
    font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 13)
    records = []
    for line, indices in enumerate(selected):
        for column, index in enumerate(indices):
            row = facts[index]
            source = profile / f'frame-{index:05d}.png'
            with Image.open(source) as image:
                payload = image.convert('RGB').crop(crop)
            x, y = column * 192, line * 232
            page.paste(payload, (x, y + 40))
            draw.text((x+2, y), f"F{index} C{row['closure']:.9f}", font=font, fill='white')
            draw.text((x+2, y+18), f"{row['eye']} pupil={row['eyeOpen']}", font=font, fill='white')
            records.append({'frame': index, 'sourcePngSha256': digest(source),
                            'actualIrisSprite': row['eye'], 'actualPupilEnabled': row['eyeOpen'],
                            'actualPupilEnabledEvidence': 'capture eyeOpen reads animator.EyeOpen; getter reads actual pupil.enabled',
                            'pupilLocalScale': None, 'pupilLocalScaleCaptured': False,
                            'irisEnabledSeparatelyCaptured': False, 'facts': row})
    path = HERE / 'eye-threshold-native-neighbours.png'
    page.save(path)
    evidence = {'status': 'CONFIRMED pre-fix threshold mismatch: open iris sprite while actual pupil.enabled is false',
                'profile': profile.name, 'manifestSha256': digest(profile / 'manifest.json'),
                'sourceCodePath': str(animator.relative_to(ROOT)), 'sourceCodeSha256BeforeFix': None,
                'sourceCodeSha256WhenEvidenceWritten': digest(animator),
                'sourceBeforeLimit': 'The old threshold lines were directly read before root edited the code. Frozen PNG and actual pupil-enabled metadata are pre-fix. The full old source file SHA is not independently captured by this helper; no new source hash is labelled old.',
                'captureHelperSha256': digest(capture),
                'thresholdCode': ['int eyeFrame = Mathf.Clamp(Mathf.RoundToInt(presentation.Closure * 3), 0, 3);',
                                  'bool open = !dead && presentation.Closure < 0.166f;',
                                  'pupil.enabled = open;',
                                  'pupil.transform.localScale = pupilRestScale * (1 + pupilWarningExpansion * presentation.EyeWarning);'],
                'scaleEvidenceLimit': 'Actual localScale is not in this frozen capture schema. Code updates scale independently of open; scale is not used to hide the pupil. Do not claim a measured actual localScale.',
                'nativeCrop': list(crop), 'pageSize': list(page.size), 'resampling': False,
                'page': path.name, 'pageSha256': digest(path), 'actuallyViewed': False,
                'frames': records}
    assert facts[90]['eye'] == facts[91]['eye'] == 'EyeOpen'
    assert facts[957]['eye'] == facts[958]['eye'] == 'EyeOpen'
    assert not facts[90]['eyeOpen'] and facts[91]['eyeOpen']
    assert not facts[957]['eyeOpen'] and facts[958]['eyeOpen']
    save(HERE / 'eye-threshold-evidence.json', evidence)
    print(json.dumps({'status': evidence['status'], 'page':str(path), 'sourceCodeSha256BeforeFix':evidence['sourceCodeSha256BeforeFix'],
                      'frames':[{key: row[key] for key in ['frame','actualIrisSprite','actualPupilEnabled','pupilLocalScaleCaptured']} for row in records],
                      'evidenceSha256':digest(HERE / 'eye-threshold-evidence.json')}, indent=2))


if __name__ == '__main__':
    main()
