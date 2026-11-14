// Animación de entrada del login:
//   1) aparece la silueta de un muñeco de palitos y volitas;
//   2) al hacer clic en él, la cámara gira 180° a su alrededor y revela una puerta;
//   3) la cámara cruza la puerta y un desvanecimiento deja ver el formulario de login.
// Solo se ejecuta si la página contiene #login-intro (la página de login lo omite tras un error).

const root = document.getElementById('login-intro');
const loginCard = document.querySelector('.login-card');

if (root) {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        dismissNow();
    } else {
        // Cancela el respaldo CSS: a partir de aquí el script controla el telón.
        root.style.animation = 'none';
        if (loginCard) loginCard.inert = true;
        runIntro().catch(error => {
            console.warn('Animación de bienvenida no disponible:', error);
            dismissNow();
        });
    }
}

function dismissNow() {
    if (loginCard) loginCard.inert = false;
    root.remove();
    document.getElementById('username')?.focus();
}

async function runIntro() {
    const THREE = await import(new URL('../lib/three/three.module.js', import.meta.url).href);

    // Paleta Insta Data Flash
    const NIGHT = 0x0A1230, MARINO = 0x16234F, CYAN = 0x22D3EE, DEEP = 0x0E7490;

    const clamp = (v, a = 0, b = 1) => Math.min(b, Math.max(a, v));
    const smooth = v => { v = clamp(v); return v * v * (3 - 2 * v); };
    const easeInOutCubic = v => (v < 0.5 ? 4 * v * v * v : 1 - Math.pow(-2 * v + 2, 3) / 2);
    const lerp = (a, b, k) => a + (b - a) * k;

    // ---------- DOM ----------
    const canvas = document.createElement('canvas');
    canvas.className = 'intro-canvas';
    root.appendChild(canvas);

    const veil = document.createElement('div');
    veil.className = 'intro-veil';
    root.appendChild(veil);

    const hint = document.createElement('button');
    hint.type = 'button';
    hint.className = 'intro-hint';
    hint.textContent = 'Haz clic en el muñeco para entrar';
    root.appendChild(hint);

    const skip = document.createElement('button');
    skip.type = 'button';
    skip.className = 'intro-skip';
    skip.textContent = 'Saltar';
    root.appendChild(skip);

    // ---------- Renderer, escena y cámara ----------
    const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
    renderer.setClearColor(NIGHT);

    const scene = new THREE.Scene();
    scene.fog = new THREE.Fog(NIGHT, 9, 30);

    const camera = new THREE.PerspectiveCamera(50, 1, 0.1, 80);

    function resize() {
        const w = window.innerWidth, h = window.innerHeight;
        renderer.setSize(w, h, false);
        camera.aspect = w / h;
        // En pantallas angostas se aleja un poco la cámara para que quepa el muñeco.
        camera.fov = camera.aspect < 0.8 ? 62 : 50;
        camera.updateProjectionMatrix();
    }
    window.addEventListener('resize', resize);
    resize();

    // ---------- Luces ----------
    scene.add(new THREE.AmbientLight(0x7f90c8, 0.7));

    const key = new THREE.DirectionalLight(0xffffff, 0.9);
    key.position.set(3, 5, 6);
    scene.add(key);

    const rim = new THREE.DirectionalLight(CYAN, 1.6);
    rim.position.set(-3, 4, -5);
    scene.add(rim);

    // ---------- Suelo, rejilla y partículas ----------
    const floor = new THREE.Mesh(
        new THREE.CircleGeometry(18, 64),
        new THREE.MeshStandardMaterial({ color: 0x0c1740, roughness: 0.95 }));
    floor.rotation.x = -Math.PI / 2;
    scene.add(floor);

    // Mismo color para las líneas centrales y el resto, para que no se dibuje una cruz bajo el muñeco.
    const grid = new THREE.GridHelper(36, 36, 0x1b2a5e, 0x1b2a5e);
    grid.position.y = 0.002;
    grid.material.transparent = true;
    grid.material.opacity = 0.3;
    scene.add(grid);

    const particleCount = 140;
    const positions = new Float32Array(particleCount * 3);
    for (let i = 0; i < particleCount; i++) {
        positions[i * 3] = (Math.random() - 0.5) * 22;
        positions[i * 3 + 1] = Math.random() * 6;
        positions[i * 3 + 2] = (Math.random() - 0.5) * 22;
    }
    const particleGeometry = new THREE.BufferGeometry();
    particleGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    const particles = new THREE.Points(
        particleGeometry,
        new THREE.PointsMaterial({ color: CYAN, size: 0.06, transparent: true, opacity: 0.7 }));
    scene.add(particles);

    // ---------- Muñeco de palitos y volitas ----------
    const bodyMat = new THREE.MeshStandardMaterial({
        color: 0x0e1a45, emissive: CYAN, emissiveIntensity: 0,
        roughness: 0.45, metalness: 0.1, transparent: true, opacity: 0,
    });
    const darkColor = new THREE.Color(0x0e1a45);
    const cyanColor = new THREE.Color(CYAN);

    const STICK_R = 0.06;
    const stick = new THREE.Group();
    scene.add(stick);

    const seg = len => {
        const m = new THREE.Mesh(new THREE.CylinderGeometry(STICK_R, STICK_R, len, 12), bodyMat);
        m.position.y = -len / 2;
        return m;
    };
    const ball = r => new THREE.Mesh(new THREE.SphereGeometry(r, 20, 16), bodyMat);

    // Cabeza y torso
    const head = ball(0.28);
    head.position.y = 2.08;
    stick.add(head);

    const torso = new THREE.Mesh(new THREE.CylinderGeometry(STICK_R, STICK_R, 0.7, 12), bodyMat);
    torso.position.y = 1.35;
    stick.add(torso);

    const shoulder = ball(0.1); shoulder.position.y = 1.7; stick.add(shoulder);
    const hip = ball(0.1); hip.position.y = 1.0; stick.add(hip);

    // Brazos y piernas con articulaciones
    function makeArm(side) {
        const pivot = new THREE.Group();
        pivot.position.set(0, 1.68, 0);
        pivot.rotation.z = side * 0.45;
        pivot.add(seg(0.5));
        const elbow = new THREE.Group();
        elbow.position.y = -0.5;
        elbow.add(ball(0.075));
        elbow.add(seg(0.45));
        const hand = ball(0.085);
        hand.position.y = -0.45;
        elbow.add(hand);
        pivot.add(elbow);
        stick.add(pivot);
        return { pivot, elbow, base: side * 0.45 };
    }

    function makeLeg(side) {
        const pivot = new THREE.Group();
        pivot.position.set(0, 1.0, 0);
        pivot.rotation.z = side * 0.12;
        pivot.add(seg(0.5));
        const knee = new THREE.Group();
        knee.position.y = -0.5;
        knee.add(ball(0.075));
        knee.add(seg(0.5));
        const foot = ball(0.09);
        foot.position.set(0, -0.5, 0.06);
        knee.add(foot);
        pivot.add(knee);
        stick.add(pivot);
        return { pivot, knee };
    }

    const armL = makeArm(-1), armR = makeArm(1);
    const legL = makeLeg(-1), legR = makeLeg(1);

    // Zona de clic generosa (invisible) alrededor del muñeco
    const hitbox = new THREE.Mesh(
        new THREE.CylinderGeometry(0.75, 0.75, 2.6, 12),
        new THREE.MeshBasicMaterial({ transparent: true, opacity: 0, depthWrite: false }));
    hitbox.position.y = 1.2;
    stick.add(hitbox);

    // ---------- Pared con puerta (detrás de la posición inicial de la cámara) ----------
    const DOOR_Z = 9;
    const doorW = 1.8, doorH = 3.0, wallW = 24, wallH = 7, wallT = 0.4;

    const wallMat = new THREE.MeshStandardMaterial({ color: MARINO, roughness: 0.85 });
    const frameMat = new THREE.MeshStandardMaterial({
        color: CYAN, emissive: CYAN, emissiveIntensity: 0.6, roughness: 0.4,
    });
    const doorMat = new THREE.MeshStandardMaterial({ color: DEEP, roughness: 0.5, metalness: 0.15 });

    const addBox = (w, h, d, mat, x, y, z, parent = scene) => {
        const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
        m.position.set(x, y, z);
        parent.add(m);
        return m;
    };

    // La pared es una sola pieza con el hueco de la puerta recortado (sin uniones ni costuras).
    const wallShape = new THREE.Shape();
    wallShape.moveTo(-wallW / 2, 0);
    wallShape.lineTo(-doorW / 2, 0);
    wallShape.lineTo(-doorW / 2, doorH);
    wallShape.lineTo(doorW / 2, doorH);
    wallShape.lineTo(doorW / 2, 0);
    wallShape.lineTo(wallW / 2, 0);
    wallShape.lineTo(wallW / 2, wallH);
    wallShape.lineTo(-wallW / 2, wallH);
    wallShape.closePath();
    const wall = new THREE.Mesh(
        new THREE.ExtrudeGeometry(wallShape, { depth: wallT, bevelEnabled: false }),
        wallMat);
    wall.position.set(0, 0, DOOR_Z - wallT / 2);
    scene.add(wall);

    addBox(0.1, doorH, wallT + 0.06, frameMat, -(doorW / 2 + 0.05), doorH / 2, DOOR_Z);
    addBox(0.1, doorH, wallT + 0.06, frameMat, doorW / 2 + 0.05, doorH / 2, DOOR_Z);
    addBox(doorW + 0.2, 0.1, wallT + 0.06, frameMat, 0, doorH + 0.05, DOOR_Z);

    // Resplandor al otro lado de la puerta
    const glow = new THREE.Mesh(
        new THREE.PlaneGeometry(8, 7),
        new THREE.MeshBasicMaterial({ color: 0xeafcff, fog: false }));
    glow.position.set(0, 3, DOOR_Z + 2.5);
    glow.rotation.y = Math.PI; // la cara visible mira hacia -z (hacia la cámara)
    scene.add(glow);

    const doorLight = new THREE.PointLight(0x9be9f7, 0, 16, 2);
    doorLight.position.set(0, 1.6, DOOR_Z - 1.2);
    scene.add(doorLight);

    // Hoja de la puerta, con bisagra a la izquierda
    const hinge = new THREE.Group();
    hinge.position.set(-doorW / 2, 0, DOOR_Z - wallT / 2 + 0.05);
    scene.add(hinge);
    addBox(doorW - 0.04, doorH - 0.02, 0.08, doorMat, doorW / 2, doorH / 2, 0, hinge);
    const handle = new THREE.Mesh(new THREE.SphereGeometry(0.07, 16, 12), frameMat);
    handle.position.set(doorW - 0.22, 1.45, -0.09);
    hinge.add(handle);

    // ---------- Interacción ----------
    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    let hovering = false;

    function pickFigure(event) {
        const rect = canvas.getBoundingClientRect();
        pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
        raycaster.setFromCamera(pointer, camera);
        return raycaster.intersectObject(hitbox, false).length > 0;
    }

    canvas.addEventListener('pointermove', e => {
        const over = phase === 'idle' && pickFigure(e);
        if (over !== hovering) {
            hovering = over;
            canvas.classList.toggle('is-hover', over);
        }
    });
    canvas.addEventListener('click', e => {
        if (phase === 'idle' && pickFigure(e)) beginOrbit();
    });
    hint.addEventListener('click', () => { if (phase === 'idle') beginOrbit(); });
    skip.addEventListener('click', () => skipIntro());

    // ---------- Línea de tiempo ----------
    const APPEAR = 2.2, ORBIT = 2.6, WALK = 3.4;
    const RADIUS = 6.5;
    const FIGURE_CENTER = new THREE.Vector3(0, 1.2, 0);
    const DOOR_TARGET = new THREE.Vector3(0, 1.5, DOOR_Z + 2);
    const lookAt = new THREE.Vector3().copy(FIGURE_CENTER);

    let phase = 'appear';
    let phaseTime = 0;
    let elapsed = 0;
    let raf = 0;
    let last = performance.now();

    camera.position.set(0, 1.7, RADIUS);
    camera.lookAt(lookAt);

    function beginOrbit() {
        phase = 'orbit';
        phaseTime = 0;
        hint.classList.remove('is-visible');
        canvas.classList.remove('is-hover');
    }

    function animateFigure(t, walkSwing = 0) {
        // Respiración suave
        stick.position.y = Math.sin(t * 1.6) * 0.015;
        head.rotation.z = Math.sin(t * 1.1) * 0.06;

        // Saludo con el brazo derecho cada pocos segundos
        const cycle = t % 5;
        const wave = cycle > 3 && cycle < 4.6 ? Math.sin(Math.PI * (cycle - 3) / 1.6) : 0;
        const swing = Math.sin(t * 7) * walkSwing;

        armR.pivot.rotation.z = armR.base + wave * 2.3;
        armR.elbow.rotation.z = wave * Math.sin(t * 12) * 0.5;
        armL.pivot.rotation.z = armL.base;
        armL.pivot.rotation.x = swing * 0.8;
        armR.pivot.rotation.x = -swing * 0.8;

        legL.pivot.rotation.x = -swing;
        legR.pivot.rotation.x = swing;
        legL.knee.rotation.x = Math.max(0, swing) * 0.7;
        legR.knee.rotation.x = Math.max(0, -swing) * 0.7;
    }

    function update(dt) {
        elapsed += dt;
        phaseTime += dt;
        particles.rotation.y += dt * 0.03;

        if (phase === 'appear') {
            // La silueta oscura aparece y luego se ilumina en cian.
            const fade = smooth(phaseTime / 1.0);
            const glowIn = smooth((phaseTime - 0.8) / 1.4);
            bodyMat.opacity = fade;
            bodyMat.color.lerpColors(darkColor, cyanColor, glowIn);
            bodyMat.emissiveIntensity = 0.3 * glowIn;
            stick.scale.setScalar(lerp(0.92, 1, fade));
            animateFigure(elapsed);
            if (phaseTime >= APPEAR) {
                phase = 'idle';
                phaseTime = 0;
                hint.classList.add('is-visible');
            }
        } else if (phase === 'idle') {
            bodyMat.emissiveIntensity = hovering ? 0.6 : 0.3;
            animateFigure(elapsed);
        } else if (phase === 'orbit') {
            const k = clamp(phaseTime / ORBIT);
            const theta = Math.PI * easeInOutCubic(k);
            camera.position.set(
                Math.sin(theta) * RADIUS,
                1.7 + Math.sin(theta) * 0.5,
                Math.cos(theta) * RADIUS);
            lookAt.copy(FIGURE_CENTER);
            animateFigure(elapsed);
            if (k >= 1) {
                phase = 'walk';
                phaseTime = 0;
            }
        } else if (phase === 'walk') {
            const k = clamp(phaseTime / WALK);

            // La cámara avanza hacia la puerta, mirando al otro lado.
            camera.position.set(0, lerp(1.7, 1.5, smooth(k)), lerp(-RADIUS, DOOR_Z - 0.3, smooth(k) * 0.6 + k * 0.4));
            lookAt.lerpVectors(FIGURE_CENTER, DOOR_TARGET, smooth(k / 0.25));

            // El muñeco se hace a un lado caminando y se desvanece.
            const turn = smooth(k / 0.12);
            const move = smooth((k - 0.1) / 0.28);
            stick.rotation.y = -Math.PI / 2 * turn;
            stick.position.x = -2.2 * move;
            animateFigure(elapsed, move > 0 && move < 1 ? 0.55 : 0);
            bodyMat.opacity = 1 - smooth((k - 0.4) / 0.2);

            // La puerta se abre y la luz del otro lado crece.
            const open = smooth((k - 0.05) / 0.55);
            hinge.rotation.y = -1.7 * open;
            doorLight.intensity = 60 * open;

            // Desvanecimiento hacia la luz.
            veil.style.opacity = String(smooth((k - 0.72) / 0.26));

            if (k >= 1) finish();
        }

        camera.lookAt(lookAt);
    }

    function frame(now) {
        if (phase === 'done') return;
        const dt = Math.min((now - last) / 1000, 0.05);
        last = now;
        update(dt);
        renderer.render(scene, camera);
        raf = requestAnimationFrame(frame);
    }

    // ---------- Final ----------
    function cleanup() {
        cancelAnimationFrame(raf);
        window.removeEventListener('resize', resize);
        scene.traverse(obj => {
            obj.geometry?.dispose();
            const mat = obj.material;
            if (Array.isArray(mat)) mat.forEach(m => m.dispose());
            else mat?.dispose();
        });
        renderer.dispose();
        renderer.forceContextLoss();
        canvas.remove();
        hint.remove();
        skip.remove();
    }

    function finish() {
        if (phase === 'done') return;
        phase = 'done';
        veil.style.opacity = '1';
        cleanup();

        // El telón (ya cubierto por la luz) se desvanece y deja ver el login.
        if (loginCard) loginCard.inert = false;
        root.classList.add('is-leaving');
        document.getElementById('username')?.focus({ preventScroll: true });
        setTimeout(() => root.remove(), 1300);
    }

    function skipIntro() {
        if (phase === 'done') return;
        hint.classList.remove('is-visible');
        veil.style.transition = 'opacity 0.45s ease';
        veil.style.opacity = '1';
        phase = 'skipping';
        setTimeout(finish, 480);
    }

    raf = requestAnimationFrame(frame);
}
