// ================================================
//  🏪 SHOP TYCOON - 3D Browser Game
//  Built with Three.js
// ================================================

// ===== CONSTANTS =====
const SHOP_W = 30, SHOP_D = 24;
const WALL_H = 3, WALL_T = 0.5;
const INTERACT_RANGE = 2.8;
const BUTTON_RANGE = 1.8;
const PLAYER_SPEED = 8;
const PLAYER_ROT_SPEED = 12;
const COLLECT_RATE = 4;   // items/sec from crate
const DEPOSIT_RATE = 3;   // items/sec to shelf
const CUSTOMER_COLORS = [0xe74c3c, 0x9b59b6, 0xe67e22, 0x1abc9c, 0xf39c12, 0xd35400, 0x8e44ad, 0x2ecc71];

// ===== GAME STATE =====
let money = 100;
let inventory = { current: 0, max: 5 };
let upgrades = { registerSpeed: 1, quality: 1, capacity: 1 };
let totalEarned = 0, customersServed = 0;
let gameStarted = false;

// Tutorial State
let tutorialStep = 1; // 1: Crate, 2: Shelf, 0: Done

// Object arrays
let shelves = [];
let registers = [];
let tycoonButtons = [];
let customers = [];
let playerStackMeshes = [];

// Timers
let collectTimer = 0, depositTimer = 0, spawnTimer = 0;
const maxCustomers = 12;
let spawnInterval = 3.5;

// ===== THREE.JS CORE =====
let scene, camera, renderer, clock;
let playerGroup, playerBody, playerLeftLeg, playerRightLeg;

// Input
const keys = {};
let joystick = { active: false, x: 0, y: 0, touchId: null };

// DOM refs
let hudMoney, hudInventory, hudCustomers, hudStats, interactionHint;
let labelsContainer, popupsContainer;
let joystickZone, joystickBase, joystickStick;
let tutorialMarker, tutorialText, emotesContainer;

// ===== MATERIALS (reusable) =====
let mats = {};

function createMaterials() {
    mats.floor = new THREE.MeshStandardMaterial({ color: 0x8fbc8f, roughness: 0.8 });
    mats.wall = new THREE.MeshStandardMaterial({ color: 0xd4c4a8, roughness: 0.6 });
    mats.player = new THREE.MeshStandardMaterial({ color: 0x3498db, roughness: 0.3, metalness: 0.2 });
    mats.crate = new THREE.MeshStandardMaterial({ color: 0x8B4513, roughness: 0.9 });
    mats.register = new THREE.MeshStandardMaterial({ color: 0x555555, roughness: 0.4, metalness: 0.3 });
    mats.button = new THREE.MeshStandardMaterial({ color: 0x00f5a0, emissive: 0x00f5a0, emissiveIntensity: 0.5, roughness: 0.3 });
    mats.buttonCantAfford = new THREE.MeshStandardMaterial({ color: 0xff4757, emissive: 0xff4757, emissiveIntensity: 0.3, roughness: 0.3 });
    mats.box = new THREE.MeshStandardMaterial({ color: 0xc0a060, roughness: 0.7 });
    mats.shelfBase = new THREE.MeshStandardMaterial({ color: 0x6d4c2a, roughness: 0.7 });
    
    // Product colors per type
    mats.products = [
        new THREE.MeshStandardMaterial({ color: 0x2ecc71, roughness: 0.5 }), // vegetables (green)
        new THREE.MeshStandardMaterial({ color: 0xe74c3c, roughness: 0.5 }), // fruits (red)
        new THREE.MeshStandardMaterial({ color: 0xf1c40f, roughness: 0.5 }), // dairy (yellow)
        new THREE.MeshStandardMaterial({ color: 0xe67e22, roughness: 0.5 }), // bakery (orange)
    ];
}

// ===== INIT THREE.JS =====
function initThree() {
    scene = new THREE.Scene();
    scene.background = new THREE.Color(0x87CEEB);
    scene.fog = new THREE.Fog(0x87CEEB, 40, 80);

    camera = new THREE.PerspectiveCamera(50, window.innerWidth / window.innerHeight, 0.1, 100);
    camera.position.set(0, 18, 16);
    camera.lookAt(0, 0, 0);

    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.toneMappingExposure = 1.2;
    document.body.appendChild(renderer.domElement);

    clock = new THREE.Clock();

    // Lighting
    const ambient = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambient);

    const sun = new THREE.DirectionalLight(0xfff4e0, 1.2);
    sun.position.set(10, 20, 10);
    sun.castShadow = true;
    sun.shadow.mapSize.set(2048, 2048);
    sun.shadow.camera.left = -20;
    sun.shadow.camera.right = 20;
    sun.shadow.camera.top = 20;
    sun.shadow.camera.bottom = -20;
    sun.shadow.camera.near = 1;
    sun.shadow.camera.far = 50;
    sun.shadow.bias = -0.001;
    scene.add(sun);

    const fill = new THREE.DirectionalLight(0x8ecae6, 0.3);
    fill.position.set(-8, 10, -5);
    scene.add(fill);

    // Resize handler
    window.addEventListener('resize', () => {
        camera.aspect = window.innerWidth / window.innerHeight;
        camera.updateProjectionMatrix();
        renderer.setSize(window.innerWidth, window.innerHeight);
    });
}

// ===== ENVIRONMENT =====
function createEnvironment() {
    // Floor
    const floorGeo = new THREE.PlaneGeometry(SHOP_W + 10, SHOP_D + 10);
    const floor = new THREE.Mesh(floorGeo, mats.floor);
    floor.rotation.x = -Math.PI / 2;
    floor.receiveShadow = true;
    scene.add(floor);

    // Grid lines on floor for visual reference
    const gridHelper = new THREE.GridHelper(SHOP_W, 30, 0x5a8a5a, 0x5a8a5a);
    gridHelper.position.y = 0.01;
    gridHelper.material.opacity = 0.15;
    gridHelper.material.transparent = true;
    scene.add(gridHelper);

    // Outer ground (grass)
    const outerGeo = new THREE.PlaneGeometry(80, 80);
    const outerMat = new THREE.MeshStandardMaterial({ color: 0x4a7c4f, roughness: 1 });
    const outer = new THREE.Mesh(outerGeo, outerMat);
    outer.rotation.x = -Math.PI / 2;
    outer.position.y = -0.05;
    outer.receiveShadow = true;
    scene.add(outer);

    // Shop floor (different color)
    const shopFloorGeo = new THREE.PlaneGeometry(SHOP_W, SHOP_D);
    const shopFloorMat = new THREE.MeshStandardMaterial({ color: 0xddd5c0, roughness: 0.6 });
    const shopFloor = new THREE.Mesh(shopFloorGeo, shopFloorMat);
    shopFloor.rotation.x = -Math.PI / 2;
    shopFloor.position.y = 0.02;
    shopFloor.receiveShadow = true;
    scene.add(shopFloor);

    // Walls
    const hw = SHOP_W / 2, hd = SHOP_D / 2;
    createWall(0, WALL_H / 2, -hd, SHOP_W + WALL_T, WALL_H, WALL_T);      // Back
    createWall(-hw, WALL_H / 2, 0, WALL_T, WALL_H, SHOP_D);                // Left
    createWall(hw, WALL_H / 2, 0, WALL_T, WALL_H, SHOP_D);                 // Right
    // Front wall with entrance gap
    createWall(-hw / 2 - 1.5, WALL_H / 2, hd, hw - 3, WALL_H, WALL_T);   // Front-left
    createWall(hw / 2 + 1.5, WALL_H / 2, hd, hw - 3, WALL_H, WALL_T);    // Front-right

    // Entrance markers
    const markerGeo = new THREE.BoxGeometry(0.3, WALL_H + 0.5, 0.3);
    const markerMat = new THREE.MeshStandardMaterial({ color: 0x00d9f5, emissive: 0x00d9f5, emissiveIntensity: 0.3 });
    const m1 = new THREE.Mesh(markerGeo, markerMat);
    m1.position.set(-3, WALL_H / 2, hd);
    m1.castShadow = true;
    scene.add(m1);
    const m2 = m1.clone();
    m2.position.set(3, WALL_H / 2, hd);
    scene.add(m2);

    // Sign above entrance
    const signGeo = new THREE.BoxGeometry(6, 1, 0.2);
    const signMat = new THREE.MeshStandardMaterial({ color: 0x2c3e50, emissive: 0x00f5a0, emissiveIntensity: 0.2 });
    const sign = new THREE.Mesh(signGeo, signMat);
    sign.position.set(0, WALL_H + 0.5, hd);
    sign.castShadow = true;
    scene.add(sign);
}

function createWall(x, y, z, w, h, d) {
    const geo = new THREE.BoxGeometry(w, h, d);
    const wall = new THREE.Mesh(geo, mats.wall);
    wall.position.set(x, y, z);
    wall.castShadow = true;
    wall.receiveShadow = true;
    scene.add(wall);
    return wall;
}

// ===== PRODUCTS =====
function createProductMesh(type) {
    const group = new THREE.Group();
    
    if (type === 0) {
        // Vegetables (Carrot)
        const bodyGeo = new THREE.ConeGeometry(0.06, 0.35, 8);
        const bodyMat = new THREE.MeshStandardMaterial({ color: 0xe67e22, roughness: 0.7 });
        const body = new THREE.Mesh(bodyGeo, bodyMat);
        body.rotation.x = Math.PI; // point down
        group.add(body);
        
        // Leaves
        const leafGeo = new THREE.CylinderGeometry(0.015, 0.015, 0.12, 4);
        const leafMat = new THREE.MeshStandardMaterial({ color: 0x2ecc71 });
        for(let i=0; i<3; i++) {
            const leaf = new THREE.Mesh(leafGeo, leafMat);
            leaf.position.y = 0.18;
            leaf.rotation.z = (Math.random() - 0.5) * 0.8;
            leaf.rotation.x = (Math.random() - 0.5) * 0.8;
            group.add(leaf);
        }
        group.rotation.x = -Math.PI / 2 + 0.2; // lay flat slightly angled
        group.position.y = 0.05;

    } else if (type === 1) {
        // Fruits (Apple)
        const appleGeo = new THREE.SphereGeometry(0.12, 12, 12);
        const appleMat = new THREE.MeshStandardMaterial({ color: 0xe74c3c, roughness: 0.4 });
        const apple = new THREE.Mesh(appleGeo, appleMat);
        apple.scale.y = 0.9;
        group.add(apple);
        
        // Stem
        const stemGeo = new THREE.CylinderGeometry(0.015, 0.015, 0.08, 4);
        const stemMat = new THREE.MeshStandardMaterial({ color: 0x5c4033 });
        const stem = new THREE.Mesh(stemGeo, stemMat);
        stem.position.y = 0.11;
        group.add(stem);
        
        // Leaf
        const leafGeo = new THREE.ConeGeometry(0.04, 0.08, 4);
        const leafMat = new THREE.MeshStandardMaterial({ color: 0x2ecc71 });
        const leaf = new THREE.Mesh(leafGeo, leafMat);
        leaf.position.set(0.04, 0.12, 0);
        leaf.rotation.z = Math.PI / 4;
        group.add(leaf);
        
        group.position.y = 0.1;

    } else if (type === 2) {
        // Dairy (Milk Carton)
        const cartonMat = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.3 });
        const bodyGeo = new THREE.BoxGeometry(0.16, 0.25, 0.16);
        const body = new THREE.Mesh(bodyGeo, cartonMat);
        body.position.y = 0.125;
        group.add(body);
        
        // Top triangle
        const topGeo = new THREE.ConeGeometry(0.113, 0.1, 4);
        const top = new THREE.Mesh(topGeo, cartonMat);
        top.rotation.y = Math.PI / 4;
        top.position.y = 0.3;
        group.add(top);
        
        // Label
        const labelGeo = new THREE.PlaneGeometry(0.12, 0.12);
        const labelMat = new THREE.MeshBasicMaterial({ color: 0x3498db });
        const label = new THREE.Mesh(labelGeo, labelMat);
        label.position.set(0, 0.12, 0.081);
        group.add(label);

    } else {
        // Bakery (Bread Loaf)
        const breadGeo = new THREE.BoxGeometry(0.25, 0.12, 0.15);
        const breadMat = new THREE.MeshStandardMaterial({ color: 0xd35400, roughness: 0.9 });
        const bread = new THREE.Mesh(breadGeo, breadMat);
        bread.position.y = 0.06;
        
        // Add cuts
        const cutGeo = new THREE.BoxGeometry(0.02, 0.13, 0.16);
        const cutMat = new THREE.MeshStandardMaterial({ color: 0xe67e22 });
        for(let i=0; i<3; i++) {
            const cut = new THREE.Mesh(cutGeo, cutMat);
            cut.position.x = -0.08 + i * 0.08;
            cut.position.y = 0.06;
            group.add(cut);
        }
        
        group.add(bread);
    }
    
    group.traverse(child => { if(child.isMesh) child.castShadow = true; });
    return group;
}

// ===== COLLISIONS =====
function checkCollisions(x, z, r) {
    const checkObj = (obj, w, d) => {
        if (!obj.active) return false;
        return (x + r > obj.x - w/2 && x - r < obj.x + w/2 &&
                z + r > obj.z - d/2 && z - r < obj.z + d/2);
    };

    // Supply Crate (fixed at -10, 8, size 2x2)
    if (x + r > -11 && x - r < -9 && z + r > 7 && z - r < 9) return true;

    // Shelves (size roughly 2.5 x 1.4)
    for (const s of shelves) {
        if (checkObj(s, 2.7, 1.6)) return true;
    }
    
    // Registers (size roughly 3 x 1.5)
    for (const reg of registers) {
        if (checkObj(reg, 3.2, 1.7)) return true;
    }
    
    return false;
}

// ===== PLAYER =====
function createPlayer() {
    playerGroup = new THREE.Group();
    playerGroup.position.set(0, 0, 9);

    // Body (cylinder)
    const bodyGeo = new THREE.CylinderGeometry(0.35, 0.4, 0.8, 12);
    playerBody = new THREE.Mesh(bodyGeo, mats.player);
    playerBody.position.y = 0.9;
    playerBody.castShadow = true;
    playerGroup.add(playerBody);

    // Head (sphere)
    const headGeo = new THREE.SphereGeometry(0.3, 12, 8);
    const head = new THREE.Mesh(headGeo, mats.player);
    head.position.y = 1.5;
    head.castShadow = true;
    playerGroup.add(head);

    // Legs
    const legGeo = new THREE.CylinderGeometry(0.12, 0.1, 0.5, 8);
    
    playerLeftLeg = new THREE.Mesh(legGeo, mats.player);
    playerLeftLeg.position.set(-0.15, 0.25, 0);
    playerLeftLeg.castShadow = true;
    playerGroup.add(playerLeftLeg);

    playerRightLeg = new THREE.Mesh(legGeo, mats.player);
    playerRightLeg.position.set(0.15, 0.25, 0);
    playerRightLeg.castShadow = true;
    playerGroup.add(playerRightLeg);

    // Shadow circle on ground
    const shadowGeo = new THREE.CircleGeometry(0.5, 16);
    const shadowMat = new THREE.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.2 });
    const shadow = new THREE.Mesh(shadowGeo, shadowMat);
    shadow.rotation.x = -Math.PI / 2;
    shadow.position.y = 0.03;
    playerGroup.add(shadow);

    scene.add(playerGroup);
}

let playerWalkAnimTimer = 0;

function updatePlayer(delta) {
    let dx = 0, dz = 0;
    
    // Keyboard input
    if (keys['w'] || keys['arrowup']) dz -= 1;
    if (keys['s'] || keys['arrowdown']) dz += 1;
    if (keys['a'] || keys['arrowleft']) dx -= 1;
    if (keys['d'] || keys['arrowright']) dx += 1;

    // Joystick input overrides
    if (joystick.active) {
        dx = joystick.x;
        dz = joystick.y;
    }

    const len = Math.sqrt(dx * dx + dz * dz);
    if (len > 0) {
        // Normalize only if using keyboard, joystick provides normalized vector length <= 1
        if (!joystick.active && len > 1) {
            dx /= len;
            dz /= len;
        }

        // Move
        let newX = playerGroup.position.x + dx * PLAYER_SPEED * delta;
        let newZ = playerGroup.position.z + dz * PLAYER_SPEED * delta;
        const radius = 0.4;

        // X Collision
        if (checkCollisions(newX, playerGroup.position.z, radius)) {
            newX = playerGroup.position.x;
        }
        // Z Collision
        if (checkCollisions(playerGroup.position.x, newZ, radius)) {
            newZ = playerGroup.position.z;
        }

        // Boundary clamping (stay in shop + entrance area)
        const hw = SHOP_W / 2 - 1, hd = SHOP_D / 2 - 1;
        playerGroup.position.x = Math.max(-hw, Math.min(hw, newX));
        // Allow going slightly outside through entrance
        if (Math.abs(newZ) < hd || (newZ > 0 && Math.abs(playerGroup.position.x) < 2.5)) {
            playerGroup.position.z = Math.max(-hd, Math.min(hd + 4, newZ));
        }

        // Rotate toward movement direction
        const targetAngle = Math.atan2(dx, dz);
        let currentAngle = playerGroup.rotation.y;
        let diff = targetAngle - currentAngle;
        while (diff > Math.PI) diff -= Math.PI * 2;
        while (diff < -Math.PI) diff += Math.PI * 2;
        playerGroup.rotation.y += diff * PLAYER_ROT_SPEED * delta;

        // Walk animation (bobbing and legs)
        playerWalkAnimTimer += delta * 15 * (len > 1 ? 1 : len); // Scale speed by joystick depth
        playerBody.position.y = 0.9 + Math.abs(Math.sin(playerWalkAnimTimer)) * 0.08;
        
        // Leg swing
        const legSwing = Math.sin(playerWalkAnimTimer) * 0.6;
        playerLeftLeg.rotation.x = legSwing;
        playerRightLeg.rotation.x = -legSwing;
        // Keep legs anchored at hip
        playerLeftLeg.position.y = 0.25 + Math.cos(playerWalkAnimTimer) * 0.05;
        playerRightLeg.position.y = 0.25 + Math.cos(playerWalkAnimTimer + Math.PI) * 0.05;

    } else {
        // Idle
        playerWalkAnimTimer = 0;
        playerBody.position.y = 0.9 + Math.sin(clock.elapsedTime * 3) * 0.03;
        playerLeftLeg.rotation.x = 0;
        playerRightLeg.rotation.x = 0;
        playerLeftLeg.position.y = 0.25;
        playerRightLeg.position.y = 0.25;
    }

    // Update inventory visual stack
    updatePlayerStack();
}

function updatePlayerStack() {
    // Remove old boxes
    for (const m of playerStackMeshes) {
        playerGroup.remove(m);
    }
    playerStackMeshes = [];

    // Add boxes based on inventory
    const boxGeo = new THREE.BoxGeometry(0.35, 0.2, 0.35);
    for (let i = 0; i < inventory.current; i++) {
        const box = new THREE.Mesh(boxGeo, mats.box);
        box.position.set(
            Math.sin(i * 0.7) * 0.05,
            2.0 + i * 0.25,
            Math.cos(i * 0.5) * 0.05
        );
        box.rotation.y = i * 0.3;
        box.castShadow = true;
        playerGroup.add(box);
        playerStackMeshes.push(box);
    }
}

// ===== CAMERA =====
let cameraHeight = 18, cameraDistance = 14;

function updateCamera(delta) {
    const target = playerGroup.position;
    const desiredPos = new THREE.Vector3(
        target.x,
        target.y + cameraHeight,
        target.z + cameraDistance
    );

    // Smoother, less erratic camera movement (lower lerp factor)
    camera.position.lerp(desiredPos, 1.5 * delta);
    
    // Always look at player roughly
    const lookTarget = new THREE.Vector3(target.x, target.y + 1, target.z);
    // You can also lerp the lookTarget if you want ultra-smooth panning, 
    // but usually just a slower position lerp is enough.
    camera.lookAt(lookTarget);
}

// ===== SUPPLY CRATE =====
let supplyCrate;

function createSupplyCrate() {
    const group = new THREE.Group();

    // Main crate box
    const crateGeo = new THREE.BoxGeometry(2, 1.5, 2);
    const crate = new THREE.Mesh(crateGeo, mats.crate);
    crate.position.y = 0.75;
    crate.castShadow = true;
    group.add(crate);

    // Cross decoration on crate
    const stripGeo = new THREE.BoxGeometry(2.05, 0.1, 0.15);
    const stripMat = new THREE.MeshStandardMaterial({ color: 0x5a3010 });
    const strip1 = new THREE.Mesh(stripGeo, stripMat);
    strip1.position.set(0, 1.0, 0);
    group.add(strip1);
    const strip2 = strip1.clone();
    strip2.rotation.y = Math.PI / 2;
    group.add(strip2);

    // Products inside (visible)
    for (let i = 0; i < 6; i++) {
        const prod = createProductMesh(Math.floor(Math.random() * 4));
        prod.position.set(
            (Math.random() - 0.5) * 1.2,
            1.5 + Math.random() * 0.3,
            (Math.random() - 0.5) * 1.2
        );
        prod.rotation.set(Math.random(), Math.random(), Math.random());
        group.add(prod);
    }

    // Label
    const labelGeo = new THREE.BoxGeometry(1.5, 0.5, 0.05);
    const labelMat = new THREE.MeshStandardMaterial({ color: 0x2c3e50, emissive: 0xffffff, emissiveIntensity: 0.1 });
    const label = new THREE.Mesh(labelGeo, labelMat);
    label.position.set(0, 2.2, 0);
    label.rotation.x = -0.3;
    group.add(label);

    group.position.set(-10, 0, 8);
    scene.add(group);
    supplyCrate = group;
}

// ===== SHELVES =====
function createShelf(x, z, productType, active = true) {
    const shelf = {
        x, z, productType, active,
        stock: active ? 5 : 0,
        maxStock: 10,
        mesh: null,
        productMeshes: [],
    };

    const group = new THREE.Group();

    // Shelf body
    const baseGeo = new THREE.BoxGeometry(2.5, 1.2, 1.2);
    const base = new THREE.Mesh(baseGeo, mats.shelfBase);
    base.position.y = 0.6;
    base.castShadow = true;
    base.receiveShadow = true;
    group.add(base);

    // Shelf top surface
    const topGeo = new THREE.BoxGeometry(2.7, 0.08, 1.4);
    const topMat = new THREE.MeshStandardMaterial({ color: 0x8b7355 });
    const top = new THREE.Mesh(topGeo, topMat);
    top.position.y = 1.2;
    group.add(top);

    // Products on shelf
    for (let i = 0; i < shelf.maxStock; i++) {
        const prod = createProductMesh(productType);
        const row = Math.floor(i / 5);
        const col = i % 5;
        prod.position.set(-1.0 + col * 0.45, 1.4 + row * 0.3, 0);
        // Add some slight randomness so it looks natural
        prod.rotation.y = (Math.random() - 0.5) * 0.5;
        prod.visible = i < shelf.stock;
        group.add(prod);
        shelf.productMeshes.push(prod);
    }

    group.position.set(x, 0, z);
    group.visible = active;
    scene.add(group);
    shelf.mesh = group;

    shelves.push(shelf);
    return shelf;
}

function updateShelfVisuals(shelf) {
    for (let i = 0; i < shelf.productMeshes.length; i++) {
        shelf.productMeshes[i].visible = i < shelf.stock;
    }
}

// ===== REGISTERS =====
function createRegister(x, z, active = true) {
    const reg = {
        x, z, active,
        queue: [],        // Array of customer objects
        processing: false,
        processTimer: 0,
        baseProcessTime: 3,
        mesh: null,
        queuePositions: [
            new THREE.Vector3(x, 0, z + 2),
            new THREE.Vector3(x, 0, z + 3.5),
            new THREE.Vector3(x, 0, z + 5),
            new THREE.Vector3(x, 0, z + 6.5),
        ],
    };

    const group = new THREE.Group();

    // Register body
    const bodyGeo = new THREE.BoxGeometry(1.8, 1.0, 1.2);
    const body = new THREE.Mesh(bodyGeo, mats.register);
    body.position.y = 0.5;
    body.castShadow = true;
    group.add(body);

    // Screen
    const screenGeo = new THREE.BoxGeometry(0.8, 0.6, 0.05);
    const screenMat = new THREE.MeshStandardMaterial({ color: 0x00d9f5, emissive: 0x00d9f5, emissiveIntensity: 0.4 });
    const screen = new THREE.Mesh(screenGeo, screenMat);
    screen.position.set(0, 1.2, -0.3);
    screen.rotation.x = -0.3;
    group.add(screen);

    // Counter surface
    const counterGeo = new THREE.BoxGeometry(3, 0.1, 1.5);
    const counterMat = new THREE.MeshStandardMaterial({ color: 0x8b8b8b });
    const counter = new THREE.Mesh(counterGeo, counterMat);
    counter.position.y = 1.0;
    counter.receiveShadow = true;
    group.add(counter);

    group.position.set(x, 0, z);
    group.visible = active;
    scene.add(group);
    reg.mesh = group;

    registers.push(reg);
    return reg;
}

function updateRegisters(delta) {
    for (const reg of registers) {
        if (!reg.active || reg.queue.length === 0) continue;

        if (!reg.processing) {
            reg.processing = true;
            reg.processTimer = 0;
        }

        const speedMult = 1 + (upgrades.registerSpeed - 1) * 0.2;
        const actualTime = reg.baseProcessTime / speedMult;

        reg.processTimer += delta;
        if (reg.processTimer >= actualTime) {
            // Process customer
            const customer = reg.queue.shift();
            if (customer) {
                const payment = customer.itemValue;
                money += payment;
                totalEarned += payment;
                customersServed++;

                showMoneyPopup(reg.x, reg.z, payment);
                pulseHud('hud-money');

                customer.state = 'leaving';
                customer.target = new THREE.Vector3(0, 0, 18);
            }

            // Shift queue forward
            for (let i = 0; i < reg.queue.length; i++) {
                reg.queue[i].target = reg.queuePositions[i].clone();
            }

            reg.processing = false;
            reg.processTimer = 0;
        }
    }
}

// ===== TYCOON BUTTONS =====
function createTycoonButton(x, z, name, cost, type, oneTime, unlockData) {
    const btn = {
        x, z, name, cost, type, oneTime, unlockData,
        active: true,
        purchased: false,
        mesh: null,
        label: null,
        cooldown: 0,
    };

    // Button mesh (flat cylinder)
    const geo = new THREE.CylinderGeometry(1.2, 1.2, 0.15, 24);
    const mesh = new THREE.Mesh(geo, mats.button.clone());
    mesh.position.set(x, 0.08, z);
    mesh.castShadow = true;
    scene.add(mesh);
    btn.mesh = mesh;

    // Glow ring
    const ringGeo = new THREE.RingGeometry(1.1, 1.3, 32);
    const ringMat = new THREE.MeshBasicMaterial({ color: 0x00f5a0, transparent: true, opacity: 0.4, side: THREE.DoubleSide });
    const ring = new THREE.Mesh(ringGeo, ringMat);
    ring.rotation.x = -Math.PI / 2;
    ring.position.set(x, 0.16, z);
    scene.add(ring);
    btn.ring = ring;

    // Create DOM label
    const labelEl = document.createElement('div');
    labelEl.className = 'button-label';
    labelEl.innerHTML = `<span class="label-name">${name}</span><span class="label-cost">$${cost}</span>`;
    labelsContainer.appendChild(labelEl);
    btn.label = labelEl;

    tycoonButtons.push(btn);
    return btn;
}

function updateButtons(delta) {
    for (const btn of tycoonButtons) {
        if (!btn.active) continue;
        btn.cooldown = Math.max(0, btn.cooldown - delta);

        // Animate glow
        const t = clock.elapsedTime;
        const canAfford = money >= btn.cost;
        btn.mesh.material.emissiveIntensity = canAfford ? 0.4 + Math.sin(t * 3) * 0.2 : 0.1;
        btn.mesh.material.color.setHex(canAfford ? 0x00f5a0 : 0xff4757);
        btn.mesh.material.emissive.setHex(canAfford ? 0x00f5a0 : 0xff4757);

        if (btn.ring) {
            btn.ring.material.opacity = canAfford ? 0.3 + Math.sin(t * 4) * 0.15 : 0.1;
            btn.ring.material.color.setHex(canAfford ? 0x00f5a0 : 0xff4757);
        }

        // Update label affordability class
        if (btn.label) {
            btn.label.classList.toggle('cant-afford', !canAfford);
            btn.label.classList.toggle('affordable', canAfford);
            btn.label.querySelector('.label-cost').textContent = `$${btn.cost}`;
        }

        // Update label position (3D to 2D)
        updateLabelPosition(btn);
    }
}

function updateLabelPosition(btn) {
    if (!btn.label || !btn.active) return;

    const pos = new THREE.Vector3(btn.x, 1.8, btn.z);
    pos.project(camera);

    if (pos.z > 1) {
        btn.label.style.display = 'none';
        return;
    }

    const x = (pos.x * 0.5 + 0.5) * window.innerWidth;
    const y = (-pos.y * 0.5 + 0.5) * window.innerHeight;

    btn.label.style.display = 'block';
    btn.label.style.left = x + 'px';
    btn.label.style.top = y + 'px';
    btn.label.style.transform = 'translate(-50%, -50%)';
}

function tryPurchase(btn) {
    if (!btn.active || btn.purchased || btn.cooldown > 0) return;
    if (money < btn.cost) return;

    money -= btn.cost;
    showMoneyPopup(btn.x, btn.z, -btn.cost);
    pulseHud('hud-money');

    switch (btn.type) {
        case 'buyShelf':
            const sd = btn.unlockData;
            const newShelf = shelves.find(s => s.x === sd.x && s.z === sd.z);
            if (newShelf) {
                newShelf.active = true;
                newShelf.mesh.visible = true;
                animateScaleIn(newShelf.mesh);
            }
            break;
        case 'buyRegister':
            const rd = btn.unlockData;
            const newReg = registers.find(r => r.x === rd.x && r.z === rd.z);
            if (newReg) {
                newReg.active = true;
                newReg.mesh.visible = true;
                animateScaleIn(newReg.mesh);
            }
            break;
        case 'upgradeSpeed':
            upgrades.registerSpeed++;
            break;
        case 'upgradeQuality':
            upgrades.quality++;
            break;
        case 'upgradeCapacity':
            upgrades.capacity++;
            inventory.max += 2;
            break;
    }

    if (btn.oneTime) {
        btn.active = false;
        btn.purchased = true;
        btn.mesh.visible = false;
        if (btn.ring) btn.ring.visible = false;
        if (btn.label) btn.label.style.display = 'none';
    } else {
        btn.cost = Math.round(btn.cost * 1.8); // Exponential scaling
        btn.cooldown = 0.8;
    }
}

function animateScaleIn(group) {
    const origScales = [];
    group.traverse(c => {
        origScales.push(c.scale.clone());
        c.scale.set(0.01, 0.01, 0.01);
    });

    let t = 0;
    const dur = 0.5;
    const anim = () => {
        t += 0.016;
        const p = Math.min(t / dur, 1);
        const ease = 1 - Math.pow(1 - p, 3); // ease-out cubic
        let i = 0;
        group.traverse(c => {
            if (i < origScales.length) {
                c.scale.lerpVectors(new THREE.Vector3(0.01, 0.01, 0.01), origScales[i], ease);
            }
            i++;
        });
        if (p < 1) requestAnimationFrame(anim);
    };
    anim();
}

// ===== CUSTOMERS =====
function createCustomerMesh() {
    const group = new THREE.Group();
    const color = CUSTOMER_COLORS[Math.floor(Math.random() * CUSTOMER_COLORS.length)];
    const mat = new THREE.MeshStandardMaterial({ color, roughness: 0.4 });

    // Body
    const bodyGeo = new THREE.CylinderGeometry(0.28, 0.32, 0.7, 8);
    const body = new THREE.Mesh(bodyGeo, mat);
    body.position.y = 0.75;
    body.castShadow = true;
    group.add(body);

    // Head
    const headGeo = new THREE.SphereGeometry(0.22, 8, 6);
    const head = new THREE.Mesh(headGeo, mat);
    head.position.y = 1.3;
    head.castShadow = true;
    group.add(head);
    
    // Legs
    const legGeo = new THREE.CylinderGeometry(0.12, 0.1, 0.4, 8);
    
    const leftLeg = new THREE.Mesh(legGeo, mat);
    leftLeg.position.set(-0.15, 0.2, 0);
    leftLeg.castShadow = true;
    group.add(leftLeg);

    const rightLeg = new THREE.Mesh(legGeo, mat);
    rightLeg.position.set(0.15, 0.2, 0);
    rightLeg.castShadow = true;
    group.add(rightLeg);

    // Keep references for animation
    group.userData = { body, head, leftLeg, rightLeg };

    return group;
}

function spawnCustomer() {
    if (customers.length >= maxCustomers) return;

    // Find a shelf with stock
    const stockedShelves = shelves.filter(s => s.active && s.stock > 0);
    if (stockedShelves.length === 0) return;

    // Find a register with space
    const availableRegs = registers.filter(r => r.active && r.queue.length < r.queuePositions.length);
    if (availableRegs.length === 0) return;

    const targetShelf = stockedShelves[Math.floor(Math.random() * stockedShelves.length)];
    const targetReg = availableRegs.reduce((a, b) => a.queue.length <= b.queue.length ? a : b);

    const mesh = createCustomerMesh();
    const spawnX = (Math.random() - 0.5) * 4;
    mesh.position.set(spawnX, 0, 16);
    scene.add(mesh);

    const customer = {
        mesh,
        state: 'goingToShelf',
        target: new THREE.Vector3(targetShelf.x + (Math.random() - 0.5) * 1.5, 0, targetShelf.z + 1.5),
        targetShelf,
        targetReg,
        speed: 2.5 + Math.random() * 1.5,
        browseTimer: 0,
        browseTime: 1.5 + Math.random() * 1.5,
        itemValue: 0,
        lifetime: 0,
    };

    customers.push(customer);
}

function updateCustomers(delta) {
    for (let i = customers.length - 1; i >= 0; i--) {
        const c = customers[i];
        c.lifetime += delta;

        // Safety timeout
        if (c.lifetime > 90) {
            removeCustomer(c, i);
            continue;
        }

        switch (c.state) {
            case 'goingToShelf':
                if (moveToward(c, delta)) {
                    c.state = 'browsing';
                    c.browseTimer = 0;
                    resetCustomerLegs(c);
                }
                break;

            case 'browsing':
                c.browseTimer += delta;
                // Slight rotation while browsing
                c.mesh.rotation.y += delta * 0.5;
                if (c.browseTimer >= c.browseTime) {
                    // Take item from shelf
                    if (c.targetShelf.stock > 0) {
                        c.targetShelf.stock--;
                        updateShelfVisuals(c.targetShelf);
                        const qualityMult = 1 + (upgrades.quality - 1) * 0.25;
                        c.itemValue = Math.round((15 + Math.random() * 5) * qualityMult);
                        
                        // Go to register
                        const queuePos = c.targetReg.queue.length;
                        if (queuePos < c.targetReg.queuePositions.length) {
                            c.target = c.targetReg.queuePositions[queuePos].clone();
                            c.targetReg.queue.push(c);
                            c.state = 'goingToRegister';
                        } else {
                            c.state = 'leaving';
                            c.target = new THREE.Vector3((Math.random() - 0.5) * 4, 0, 18);
                        }
                    } else {
                        // Shelf is empty! Wait a bit.
                        c.state = 'waitingAtShelf';
                        c.waitTime = 0;
                        resetCustomerLegs(c);
                    }
                }
                break;

            case 'waitingAtShelf':
                c.waitTime += delta;
                if (c.targetShelf.stock > 0) {
                    // Item restocked! Grab it.
                    c.targetShelf.stock--;
                    updateShelfVisuals(c.targetShelf);
                    const qualityMult = 1 + (upgrades.quality - 1) * 0.25;
                    c.itemValue = Math.round((15 + Math.random() * 5) * qualityMult);
                    
                    const queuePos = c.targetReg.queue.length;
                    if (queuePos < c.targetReg.queuePositions.length) {
                        c.target = c.targetReg.queuePositions[queuePos].clone();
                        c.targetReg.queue.push(c);
                        c.state = 'goingToRegister';
                    } else {
                        c.state = 'leaving';
                        c.target = new THREE.Vector3((Math.random() - 0.5) * 4, 0, 18);
                    }
                } else if (c.waitTime > 5) {
                    // Waited too long
                    showEmote(c.mesh.position, Math.random() > 0.5 ? '😠' : '😢');
                    c.state = 'leavingAngry';
                    c.target = new THREE.Vector3((Math.random() - 0.5) * 4, 0, 18);
                }
                break;
            
            case 'leavingAngry':
                if (moveToward(c, delta)) {
                    removeCustomer(c, i);
                }
                break;

            case 'goingToRegister':
                if (moveToward(c, delta)) {
                    c.state = 'waitingInQueue';
                    resetCustomerLegs(c);
                }
                break;

            case 'waitingInQueue':
                // Move to current queue position if shifted
                if (c.target) {
                    const reached = moveToward(c, delta);
                    if (reached) resetCustomerLegs(c);
                }
                break;

            case 'leaving':
                if (moveToward(c, delta)) {
                    removeCustomer(c, i);
                }
                break;
        }
    }
}

function moveToward(customer, delta) {
    const pos = customer.mesh.position;
    const target = customer.target;
    const dx = target.x - pos.x;
    const dz = target.z - pos.z;
    const dist = Math.sqrt(dx * dx + dz * dz);

    if (dist < 0.1) return true;

    const speed = customer.speed * delta;
    const nx = dx / dist, nz = dz / dist;
    pos.x += nx * Math.min(speed, dist);
    pos.z += nz * Math.min(speed, dist);

    // Face movement direction
    const angle = Math.atan2(nx, nz);
    customer.mesh.rotation.y = angle;

    // Walk bob animation
    const data = customer.mesh.userData;
    const animTime = customer.lifetime * 12;
    data.body.position.y = 0.75 + Math.abs(Math.sin(animTime)) * 0.08;
    
    // Leg swing
    const legSwing = Math.sin(animTime) * 0.6;
    data.leftLeg.rotation.x = legSwing;
    data.rightLeg.rotation.x = -legSwing;
    data.leftLeg.position.y = 0.2 + Math.cos(animTime) * 0.05;
    data.rightLeg.position.y = 0.2 + Math.cos(animTime + Math.PI) * 0.05;

    return false;
}

function resetCustomerLegs(customer) {
    const data = customer.mesh.userData;
    data.body.position.y = 0.75;
    data.leftLeg.rotation.x = 0;
    data.rightLeg.rotation.x = 0;
    data.leftLeg.position.y = 0.2;
    data.rightLeg.position.y = 0.2;
}

function removeCustomer(customer, index) {
    scene.remove(customer.mesh);
    // Remove from register queue if present
    for (const reg of registers) {
        const qi = reg.queue.indexOf(customer);
        if (qi !== -1) {
            reg.queue.splice(qi, 1);
            // Shift remaining
            for (let j = 0; j < reg.queue.length; j++) {
                reg.queue[j].target = reg.queuePositions[j].clone();
            }
        }
    }
    customers.splice(index, 1);
}

// ===== INTERACTIONS =====
function checkInteractions(delta) {
    const px = playerGroup.position.x;
    const pz = playerGroup.position.z;
    let hint = '';

    // --- Supply Crate ---
    const crateX = supplyCrate.position.x, crateZ = supplyCrate.position.z;
    const crateDist = Math.sqrt((px - crateX) ** 2 + (pz - crateZ) ** 2);
    if (crateDist < INTERACT_RANGE) {
        if (inventory.current < inventory.max) {
            collectTimer += delta;
            if (collectTimer >= 1 / COLLECT_RATE) {
                collectTimer = 0;
                inventory.current = Math.min(inventory.current + 1, inventory.max);
                updatePlayerStack();
            }
            hint = `📦 אוסף סחורה... (${inventory.current}/${inventory.max})`;
        } else {
            hint = '📦 מלא! לך למלא מדפים';
        }
    } else {
        collectTimer = 0;
    }

    // --- Shelves ---
    let nearShelf = false;
    for (const shelf of shelves) {
        if (!shelf.active) continue;
        const sd = Math.sqrt((px - shelf.x) ** 2 + (pz - shelf.z) ** 2);
        if (sd < INTERACT_RANGE) {
            nearShelf = true;
            if (inventory.current > 0 && shelf.stock < shelf.maxStock) {
                depositTimer += delta;
                if (depositTimer >= 1 / DEPOSIT_RATE) {
                    depositTimer = 0;
                    shelf.stock++;
                    inventory.current--;
                    updateShelfVisuals(shelf);
                    updatePlayerStack();
                }
                hint = `🗄️ ממלא מדף... (${shelf.stock}/${shelf.maxStock})`;
            } else if (shelf.stock >= shelf.maxStock) {
                hint = '🗄️ המדף מלא!';
            } else if (inventory.current <= 0) {
                hint = '🗄️ אין לך סחורה - לך לארגז!';
            }
        }
    }
    if (!nearShelf) depositTimer = 0;

    // --- Tycoon Buttons ---
    for (const btn of tycoonButtons) {
        if (!btn.active) continue;
        const bd = Math.sqrt((px - btn.x) ** 2 + (pz - btn.z) ** 2);
        if (bd < BUTTON_RANGE) {
            if (money >= btn.cost) {
                tryPurchase(btn);
                hint = `✅ ${btn.name}!`;
            } else {
                hint = `❌ צריך $${btn.cost} (יש לך $${money})`;
            }
        }
    }

    // Update hint
    if (hint) {
        interactionHint.textContent = hint;
        interactionHint.style.display = 'block';
    } else {
        interactionHint.style.display = 'none';
    }
}

// ===== TUTORIAL & EMOTES =====
function updateTutorial() {
    if (tutorialStep === 0) {
        if (tutorialMarker) tutorialMarker.style.display = 'none';
        return;
    }
    
    if (tutorialStep === 1) {
        if (inventory.current > 0) {
            tutorialStep = 2; 
        } else {
            positionTutorialMarker(supplyCrate.position.x, 2.5, supplyCrate.position.z, 'אסוף סחורה כאן');
        }
    }
    
    if (tutorialStep === 2) {
        const firstShelf = shelves[0];
        if (firstShelf && firstShelf.stock > 0) {
            tutorialStep = 0; 
            if (tutorialMarker) tutorialMarker.style.display = 'none';
        } else if (firstShelf) {
            positionTutorialMarker(firstShelf.x, 2.5, firstShelf.z, 'מלא את המדף כאן');
        }
    }
}

function positionTutorialMarker(worldX, worldY, worldZ, text) {
    if (!tutorialMarker || !tutorialText) return;
    
    const pos = new THREE.Vector3(worldX, worldY, worldZ);
    pos.project(camera);
    
    if (pos.z > 1) {
        tutorialMarker.style.display = 'none';
        return;
    }
    
    const x = (pos.x * 0.5 + 0.5) * window.innerWidth;
    const y = (-pos.y * 0.5 + 0.5) * window.innerHeight;
    
    tutorialMarker.style.display = 'block';
    tutorialMarker.style.left = x + 'px';
    tutorialMarker.style.top = y + 'px';
    tutorialText.textContent = text;
}

function showEmote(worldPos, emoji) {
    if (!emotesContainer) return;
    const pos = worldPos.clone();
    pos.y += 2.5;
    pos.project(camera);
    
    if (pos.z > 1) return;
    
    const x = (pos.x * 0.5 + 0.5) * window.innerWidth;
    const y = (-pos.y * 0.5 + 0.5) * window.innerHeight;
    
    const emote = document.createElement('div');
    emote.className = 'customer-emote';
    emote.textContent = emoji;
    emote.style.left = x + 'px';
    emote.style.top = y + 'px';
    emotesContainer.appendChild(emote);
    
    setTimeout(() => emote.remove(), 3000);
}

// ===== UI =====
function updateUI() {
    hudMoney.textContent = `💰 $${money.toLocaleString()}`;
    hudInventory.textContent = `📦 ${inventory.current}/${inventory.max}`;
    hudCustomers.textContent = `👥 ${customers.length}/${maxCustomers}`;
    hudStats.textContent = `⚡${upgrades.registerSpeed} | ⭐${upgrades.quality} | 🎒${upgrades.capacity}`;

    // Color inventory based on fullness
    if (inventory.current >= inventory.max) {
        hudInventory.style.borderColor = 'rgba(255, 71, 87, 0.5)';
    } else if (inventory.current > inventory.max * 0.6) {
        hudInventory.style.borderColor = 'rgba(255, 165, 2, 0.5)';
    } else {
        hudInventory.style.borderColor = 'rgba(255,255,255,0.12)';
    }
}

function showMoneyPopup(worldX, worldZ, amount) {
    const pos = new THREE.Vector3(worldX, 2.5, worldZ);
    pos.project(camera);

    const x = (pos.x * 0.5 + 0.5) * window.innerWidth;
    const y = (-pos.y * 0.5 + 0.5) * window.innerHeight;

    const popup = document.createElement('div');
    popup.className = 'money-popup' + (amount < 0 ? ' negative' : '');
    popup.textContent = amount > 0 ? `+$${amount}` : `-$${Math.abs(amount)}`;
    popup.style.left = x + 'px';
    popup.style.top = y + 'px';
    popupsContainer.appendChild(popup);

    setTimeout(() => popup.remove(), 1500);
}

function pulseHud(id) {
    const el = document.getElementById(id);
    if (el) {
        el.classList.remove('pulse');
        void el.offsetWidth; // trigger reflow
        el.classList.add('pulse');
    }
}

// ===== INPUT =====
function initInput() {
    window.addEventListener('keydown', e => {
        keys[e.key.toLowerCase()] = true;
    });
    window.addEventListener('keyup', e => {
        keys[e.key.toLowerCase()] = false;
    });
    window.addEventListener('wheel', e => {
        cameraHeight += e.deltaY * 0.01;
        cameraHeight = Math.max(8, Math.min(30, cameraHeight));
        cameraDistance = cameraHeight * 0.78;
    });

    // Joystick Touch Input
    if (joystickZone && joystickBase && joystickStick) {
        let baseOriginX = 0;
        let baseOriginY = 0;
        const maxRadius = 60; // Half of the 120px base

        const resetJoystick = () => {
            joystick.active = false;
            joystick.x = 0;
            joystick.y = 0;
            joystick.touchId = null;
            joystickStick.style.transform = `translate(-50%, -50%)`;
            joystickBase.style.display = 'none';
        };

        const handleTouchMove = (e) => {
            if (!joystick.active) return;
            
            // Find our specific touch
            let touch = null;
            for(let i=0; i<e.changedTouches.length; i++) {
                if(e.changedTouches[i].identifier === joystick.touchId) {
                    touch = e.changedTouches[i];
                    break;
                }
            }
            if (!touch) return;

            let dx = touch.clientX - baseOriginX;
            let dy = touch.clientY - baseOriginY;
            const distance = Math.sqrt(dx * dx + dy * dy);

            if (distance > maxRadius) {
                dx = (dx / distance) * maxRadius;
                dy = (dy / distance) * maxRadius;
            }

            joystickStick.style.transform = `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px))`;
            
            // Normalize for movement (-1 to 1)
            joystick.x = dx / maxRadius;
            joystick.y = dy / maxRadius;
        };

        joystickZone.addEventListener('touchstart', (e) => {
            e.preventDefault();
            if (joystick.active) return; // already active
            
            const touch = e.changedTouches[0];
            joystick.active = true;
            joystick.touchId = touch.identifier;
            
            // Set dynamic position
            baseOriginX = touch.clientX;
            baseOriginY = touch.clientY;
            joystickBase.style.left = baseOriginX + 'px';
            joystickBase.style.top = baseOriginY + 'px';
            joystickBase.style.display = 'block';
            
            handleTouchMove(e);
        }, { passive: false });

        joystickZone.addEventListener('touchmove', (e) => {
            e.preventDefault();
            handleTouchMove(e);
        }, { passive: false });

        const endTouch = (e) => {
            for(let i=0; i<e.changedTouches.length; i++) {
                if(e.changedTouches[i].identifier === joystick.touchId) {
                    resetJoystick();
                    break;
                }
            }
        };

        joystickZone.addEventListener('touchend', endTouch);
        joystickZone.addEventListener('touchcancel', endTouch);
    }
}

// ===== CUSTOMER SPAWNING =====
function updateSpawner(delta) {
    spawnTimer += delta;
    if (spawnTimer >= spawnInterval) {
        spawnTimer = 0;
        spawnCustomer();
        // Randomize next spawn
        spawnInterval = 2.5 + Math.random() * 2;
    }
}

// ===== GAME LOOP =====
function animate() {
    requestAnimationFrame(animate);
    if (!renderer || !scene || !camera) return;

    const delta = Math.min(clock.getDelta(), 0.05); // cap delta

    if (gameStarted) {
        try {
            updatePlayer(delta);
            updateCamera(delta);
            checkInteractions(delta);
            updateTutorial();
            updateCustomers(delta);
            updateRegisters(delta);
            updateButtons(delta);
            updateSpawner(delta);
            updateUI();
        } catch (e) {
            console.error('Game loop error:', e);
        }
    }

    renderer.render(scene, camera);
}

// ===== SETUP SHOP =====
function setupShop() {
    // Supply crate
    createSupplyCrate();

    // Initial shelves (active)
    createShelf(-5, -1, 0, true);   // Vegetables
    createShelf(5, -1, 1, true);    // Fruits

    // Buyable shelves (inactive)
    createShelf(-5, -7, 2, false);  // Dairy
    createShelf(5, -7, 3, false);   // Bakery

    // Initial register (active)
    createRegister(7, 4, true);

    // Buyable register (inactive)
    createRegister(-7, 4, false);

    // === TYCOON BUTTONS ===
    // Buy shelves
    createTycoonButton(-5, -9.5, '🗄️ קנה מדף חלב', 200, 'buyShelf', true, { x: -5, z: -7 });
    createTycoonButton(5, -9.5, '🗄️ קנה מדף מאפים', 400, 'buyShelf', true, { x: 5, z: -7 });

    // Buy register
    createTycoonButton(-7, 7, '🧾 קנה קופה', 300, 'buyRegister', true, { x: -7, z: 4 });

    // Repeatable upgrades (Cheaper base cost)
    createTycoonButton(12, -7, '⚡ שדרג מהירות קופה', 100, 'upgradeSpeed', false, null);
    createTycoonButton(12, -3, '⭐ שדרג איכות מוצרים', 150, 'upgradeQuality', false, null);
    createTycoonButton(12, 1, '🎒 שדרג קיבולת', 50, 'upgradeCapacity', false, null);
}

// ===== INIT =====
function init() {
    // Check if Three.js loaded
    if (typeof THREE === 'undefined') {
        document.body.innerHTML = '<div style="color:white;text-align:center;padding:50px;font-family:sans-serif;background:#1a1a2e;height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center"><h1>⚠️ Three.js failed to load</h1><p>Please check your internet connection and refresh the page.</p></div>';
        return;
    }

    // DOM refs
    hudMoney = document.getElementById('hud-money');
    hudInventory = document.getElementById('hud-inventory');
    hudCustomers = document.getElementById('hud-customers');
    hudStats = document.getElementById('hud-stats');
    interactionHint = document.getElementById('interaction-hint');
    labelsContainer = document.getElementById('labels-container');
    popupsContainer = document.getElementById('popups-container');
    
    // Joystick DOM refs
    joystickZone = document.getElementById('joystick-zone');
    joystickBase = document.getElementById('joystick-base');
    joystickStick = document.getElementById('joystick-stick');
    
    // Tutorial & Emotes
    tutorialMarker = document.getElementById('tutorial-marker');
    tutorialText = document.getElementById('tutorial-text');
    emotesContainer = document.getElementById('emotes-container');

    try {
        createMaterials();
        initThree();
        createEnvironment();
        createPlayer();
        setupShop();
        initInput();
        animate();
    } catch (e) {
        console.error('Init error:', e);
        document.body.innerHTML = '<div style="color:white;text-align:center;padding:50px;font-family:sans-serif;background:#1a1a2e;height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center"><h1>⚠️ Error initializing game</h1><p>' + e.message + '</p></div>';
    }
}

// ===== START GAME =====
function startGame() {
    document.getElementById('start-screen').style.display = 'none';
    document.getElementById('hud').style.display = 'flex';
    gameStarted = true;
    clock.start();
}

// Run on page load
window.addEventListener('DOMContentLoaded', init);
