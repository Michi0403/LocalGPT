(function () {
    'use strict';

    // javascript-diagnostics: guarded

    // Pure presentation. The C# GameSceneEngine owns ticks, collisions and authoritative time.
    // Scenery, sprites, camera speed and color phases always come from the current project draft.
    function draw(canvasId, scene, clock) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!(canvas instanceof HTMLCanvasElement) || !scene || !clock) return;
            const width = Math.max(8, Math.min(240, scene.viewWidth || 32));
            const height = Math.max(6, Math.min(100, scene.viewHeight || 18));
            const cell = 16;
            canvas.width = width * cell;
            canvas.height = height * cell;
            const ctx = canvas.getContext('2d', { alpha: false });
            if (!ctx) return;

            const worldTime = Number(clock.worldSeconds) || 0;
            const cameraX = Number(clock.cameraX) || 0;
            const cameraY = Number(clock.cameraY) || 0;
            let fg = '#eeeeee';
            let bg = '#101820';
            for (const key of [...(scene.paletteTimeline || [])].sort((a, b) => a.atWorldSecond - b.atWorldSecond)) {
                if (key.atWorldSecond > worldTime) break;
                fg = key.foreground || fg;
                bg = key.background || bg;
            }
            ctx.fillStyle = bg;
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.font = 'bold 14px monospace';
            ctx.textBaseline = 'top';
            const mode = scene.artworkMode || 'ClassicGlyph';
            const variables = clock.sceneVariables || {};
            function expand(template) {
                let value = String(template || '').replaceAll('{tick}', String(clock.tick || 0))
                    .replaceAll('{worldSeconds}', worldTime.toFixed(2))
                    .replaceAll('{turn}', String(clock.currentTurn || 0));
                for (const [key, replacement] of Object.entries(variables)) {
                    if (key.length > 80) continue;
                    value = value.split('{' + key + '}').join(String(replacement || '').replace(/\s+/g, ' ').slice(0, 160));
                }
                return value.slice(0, 4096);
            }
            function drawTextRows(rows, x, y, color) {
                ctx.fillStyle = color || fg;
                for (let row = 0; row < rows.length; row++) {
                    if (y + row < 0 || y + row >= height) continue;
                    const text = expand(rows[row]);
                    for (let column = 0; column < text.length; column++) {
                        if (x + column >= 0 && x + column < width && text[column] !== ' ')
                            ctx.fillText(text[column], (x + column) * cell, (y + row) * cell);
                    }
                }
            }
            if (mode !== 'ClassicGlyph') {
                for (const screen of scene.textCanvases || []) {
                    const value = variables[screen.visibleWhenVariable];
                    if ((screen.fromWorldSecond || 0) > worldTime
                        || (screen.untilWorldSecond || 0) > 0 && worldTime >= screen.untilWorldSecond
                        || screen.visibleWhenVariable && value !== screen.visibleWhenValue)
                        continue;
                    drawTextRows(screen.rows || [], screen.x || 0, screen.y || 0, fg);
                }
            }

            for (const layer of [...(scene.layers || [])].sort((a, b) => a.depth - b.depth)) {
                const pattern = layer.glyphPattern || '';
                if (!pattern.length) continue;
                ctx.fillStyle = layer.color || fg;
                const scrollX = Math.floor(worldTime * (layer.scrollCellsPerWorldSecondX || 0) + cameraX * (layer.cameraFactorX || 0));
                const scrollY = Math.floor(worldTime * (layer.scrollCellsPerWorldSecondY || 0) + cameraY * (layer.cameraFactorY || 0));
                const lastRow = Math.min(height - 1, layer.lastScreenRow ?? height - 1);
                for (let row = Math.max(0, layer.firstScreenRow || 0); row <= lastRow; row++) {
                    for (let x = 0; x < width; x++) {
                        const location = x + scrollX + (row + scrollY) * width;
                        if (layer.wrap === false && (location < 0 || location >= pattern.length)) continue;
                        const index = ((location % pattern.length) + pattern.length) % pattern.length;
                        if (pattern[index] !== ' ') ctx.fillText(pattern[index], x * cell, row * cell);
                    }
                }
            }

            const sprites = new Map((scene.sprites || []).map(sprite => [sprite.id, sprite]));
            function animatedSpriteId(object) {
                let selected = object.spriteId;
                const animation = (scene.animations || []).find(item => item.targetId === object.id);
                if (!animation || !animation.frames || !animation.frames.length || worldTime < animation.startWorldSecond) return selected;
                const duration = animation.frames.reduce((sum, frame) => sum + (frame.durationWorldSeconds || 0), 0);
                if (duration <= 0) return selected;
                let phase = worldTime - animation.startWorldSecond;
                phase = animation.loop === false ? Math.min(phase, Math.max(0, duration - 0.000001)) : phase % duration;
                for (const frame of animation.frames) {
                    phase -= frame.durationWorldSeconds;
                    if (phase < 0) return frame.spriteId;
                }
                return selected;
            }
            function drawObject(object, x, y) {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                const sprite = sprites.get(animatedSpriteId(object));
                if (sprite && mode !== 'ClassicGlyph' && mode !== 'AuthoredPixel' && sprite.asciiRows && sprite.asciiRows.length) {
                    drawTextRows(sprite.asciiRows, x, y, object.color || fg);
                } else if (sprite && sprite.pixelRows && sprite.pixelRows.length) {
                    const pxSize = Math.max(1, Math.floor(cell / Math.max(1, ...sprite.pixelRows.map(row => row.length), sprite.pixelRows.length)));
                    for (let row = 0; row < sprite.pixelRows.length; row++) {
                        for (let column = 0; column < sprite.pixelRows[row].length; column++) {
                            const code = sprite.pixelRows[row][column];
                            const color = sprite.palette && sprite.palette[code];
                            if (!color || code === ' ') continue;
                            ctx.fillStyle = color;
                            ctx.fillRect(x * cell + column * pxSize, y * cell + row * pxSize, pxSize, pxSize);
                        }
                    }
                } else {
                    ctx.fillStyle = object.color || fg;
                    ctx.fillText((object.glyph || '?').slice(0, 1), x * cell, y * cell);
                }
            }

            for (const tile of scene.tiles || [])
                drawObject(tile, tile.x - Math.floor(cameraX), tile.y - Math.floor(cameraY));
            for (const actor of [...(scene.actors || [])].sort((a, b) => (a.layer || 0) - (b.layer || 0))) {
                const position = (clock.actorPositions && clock.actorPositions[actor.id]) || actor;
                drawObject(actor, Math.floor(position.x - cameraX), Math.floor(position.y - cameraY));
            }
            for (const hud of scene.hud || []) {
                ctx.fillStyle = hud.color || fg;
                const label = expand(hud.format || '');
                ctx.fillText(label, hud.x * cell, hud.y * cell);
            }
        } catch (error) {
            console.error('[LocalGPT/GameStudio] Pixel projection failed; ASCII fallback remains available.', error);
        }
    }

    window.localgptGameStudio = Object.freeze({ draw });
})();
