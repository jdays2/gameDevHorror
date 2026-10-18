// Run from the Unity project directory. Requires sharp.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const source = path.resolve('Concepts/Used/UI/GameplayHud03/Sources');
const output = path.resolve('Assets/EmergencyShift/Art/UI/GameplayHud03');

async function prepare() {
    const pistol = fs.readFileSync(path.join(source, 'pistol.svg'));
    await sharp(pistol).trim({background:'#00000000'}).resize({width:256}).png().toFile(path.join(output, 'pistol.png'));

    const shotgun = fs.readFileSync(path.join(source, 'pump-shotgun-hud.svg'));
    await sharp(shotgun).trim({background:'#00000000'}).resize({width:384}).png().toFile(path.join(output, 'shotgun.png'));

    for (const name of ['flashlight-on', 'flashlight-off']) {
        const vector = fs.readFileSync(path.join(source, name + '-flat.svg'));
        await sharp(vector).resize(128, 128).png().toFile(path.join(output, name + '.png'));
    }
}

prepare().catch(error => {
    console.error(error);
    process.exitCode = 1;
});
