# FPS Weapon System: инструкция для начинающих

Эта инструкция описывает, как пользоваться FPS-системой оружия в проекте `shooter3d`.

Система находится в папке:

```text
Assets/Scripts/FPSWeapons
```

Текущий prefab оружия:

```text
Assets/HandRailGun/HandRailGun.prefab
```

## Что уже сделано

В проект добавлена FPS-система оружия, которая стреляет не физическими пулями, а через `Raycast`. Это стандартный подход для большинства FPS: при выстреле из камеры выпускается невидимый луч, и если он попадает в объект, скрипт сразу обрабатывает урон и эффекты попадания.

У `HandRailGun` уже добавлены основные компоненты:

- `FpsWeaponViewSetup`
- `FpsWeaponController`
- `WeaponImpactSystem`
- `WeaponRecoil`

При запуске игры `FpsWeaponViewSetup` автоматически:

- ищет камеру игрока;
- создает дочернюю `Weapon Camera`;
- переводит оружие на layer `Weapon`;
- исключает layer `Weapon` из основной камеры;
- настраивает `Weapon Camera` так, чтобы она рисовала только оружие;
- подключает `Weapon Camera` в URP camera stack;
- отключает старый скрипт `Shoot` под оружием, чтобы не было двойной стрельбы.

## Основная идея

В FPS часто используют две камеры:

- `Main Camera` рисует мир, врагов, стены, пол, UI.
- `Weapon Camera` рисует только оружие.

Так оружие не проваливается визуально в стены. Оно остается поверх мира, потому что его рисует отдельная камера.

Важно: это не значит, что оружие физически проходит сквозь стены. Обычно в FPS оружие в руках игрока вообще не участвует в физике мира. Стреляет не mesh оружия, а луч из камеры.

## Структура игрока

Ожидаемая структура в Hierarchy:

```text
player
  Main Camera
    HandRailGun
      ShootPoint
```

В текущей сцене оружие уже находится под камерой игрока. Если ты создаешь нового игрока, сделай так же: перетащи `HandRailGun` внутрь `Main Camera`, чтобы оружие двигалось вместе с камерой.

## Layer Weapon

В проекте уже создан layer:

```text
Weapon
```

Он нужен, чтобы отделить рендер оружия от рендера мира.

Проверка в Unity:

1. Открой `Edit > Project Settings > Tags and Layers`.
2. В списке Layers должен быть `Weapon`.
3. Если его нет, добавь его вручную в свободный слот.

## Компонент FpsWeaponViewSetup

Этот компонент отвечает за отображение оружия отдельной камерой.

Он должен висеть на корневом объекте оружия, например на `HandRailGun`.

Поля в Inspector:

- `World Camera` - основная камера игрока. Можно оставить пустым, скрипт найдет ее сам.
- `Weapon Camera` - отдельная камера оружия. Можно оставить пустым, скрипт создаст ее сам.
- `Weapon Layer Name` - имя слоя оружия. Должно быть `Weapon`.
- `Near Clip Plane` - обычно `0.01`. Чем меньше значение, тем ближе камера может видеть оружие.
- `Far Clip Plane` - обычно `30`. Оружию не нужна большая дальность рендера.
- `Disable Weapon Colliders` - лучше оставить включенным.
- `Disable Legacy Shoot` - лучше оставить включенным, чтобы старый `Shoot` не стрелял вместе с новой системой.

## Компонент FpsWeaponController

Это главный скрипт стрельбы.

Он отвечает за:

- нажатие левой кнопки мыши;
- режим стрельбы automatic/semi-auto;
- Raycast;
- урон по `Health`;
- отдачу;
- muzzle flash;
- tracer;
- impact effects;
- shell ejection.

Основные поля:

- `Fire Mode` - режим стрельбы.
  - `Automatic`: оружие стреляет, пока зажата левая кнопка мыши.
  - `SemiAuto`: один выстрел на одно нажатие.
- `Aim Camera` - камера, из которой идет Raycast. Можно оставить пустым, если оружие под камерой игрока.
- `Muzzle Point` - точка у дула оружия. Скрипт может найти `ShootPoint`, `MuzzlePoint`, `Muzzle` или `FirePoint` автоматически.
- `Hit Mask` - слои, по которым может попадать выстрел. Сейчас `Weapon` исключен, чтобы оружие не стреляло само в себя.
- `Damage` - урон за выстрел.
- `Range` - дальность выстрела.
- `Rounds Per Minute` - скорострельность.
- `Spread Angle` - разброс в градусах.
- `Muzzle Flash` - Particle System вспышки выстрела.
- `Tracer Prefab` - prefab следа пули.
- `Impact System` - ссылка на `WeaponImpactSystem`.
- `Recoil` - ссылка на `WeaponRecoil`.
- `Camera Shake` - опциональный shake камеры.

## ShootPoint / MuzzlePoint

`ShootPoint` - это пустой GameObject около дула оружия.

Он нужен для визуальных эффектов:

- откуда начинается tracer;
- где появляется muzzle flash;
- откуда примерно "вылетает" гильза, если ты добавишь shell ejection.

Важно: попадание считается Raycast-лучом из камеры игрока, а не из `ShootPoint`. Это сделано специально, чтобы игрок попадал туда, куда смотрит центр экрана.

## Отдача оружия

За отдачу отвечает `WeaponRecoil`.

Основные поля:

- `Weapon Root` - transform оружия, который будет дергаться при выстреле.
- `Camera Root` - опционально, если хочешь отдачу камеры.
- `Position Kick` - сдвиг оружия назад/вниз при выстреле.
- `Vertical Recoil` - вертикальный подброс.
- `Horizontal Recoil` - случайный горизонтальный увод.
- `Roll Recoil` - небольшой наклон вокруг оси Z.
- `Return Speed` - скорость возврата в исходное положение.
- `Snappiness` - резкость отдачи.

Для начала можно использовать такие значения:

```text
Position Kick: X 0, Y -0.015, Z -0.06
Vertical Recoil: 2.2
Horizontal Recoil: 0.75
Roll Recoil: 1
Return Speed: 14
Snappiness: 24
```

Если отдача слишком сильная, уменьши `Vertical Recoil` и `Position Kick.z`.

## Muzzle Flash

Muzzle flash - это вспышка у дула при выстреле.

Как настроить:

1. Создай или найди prefab Particle System вспышки.
2. Помести его около `ShootPoint` или сделай дочерним объектом оружия.
3. В `FpsWeaponController` перетащи Particle System в поле `Muzzle Flash`.
4. В Particle System отключи `Play On Awake`.

Рекомендации:

- Duration: `0.03-0.08`
- Start Lifetime: `0.03-0.08`
- Looping: off
- Play On Awake: off
- Simulation Space: Local, если эффект дочерний объект оружия

## Bullet Tracer

Tracer - это короткая линия/след, показывающая путь выстрела.

Для tracer нужен prefab с:

- `LineRenderer`;
- компонентом `FpsTracer`.

Настройка:

1. Создай empty GameObject `BulletTracer`.
2. Добавь `LineRenderer`.
3. Добавь `FpsTracer`.
4. Настрой материал и ширину линии.
5. Сделай prefab.
6. Перетащи prefab в поле `Tracer Prefab` у `FpsWeaponController`.

Пример настроек `LineRenderer`:

```text
Width: 0.015
Position Count: 2
Material: яркий unlit material
Use World Space: true
```

`Tracer Lifetime` обычно ставят `0.04-0.1`.

## Impact Effects

Impact effects - это эффекты попадания:

- искры по металлу;
- пыль по бетону;
- кровь по живым существам;
- искры/дым по механическим врагам.

За это отвечают:

- `WeaponImpactSystem`
- `SurfaceEffectProfile`
- `SurfaceIdentifier`
- `WeaponSurfaceType`

## SurfaceEffectProfile

`SurfaceEffectProfile` - это asset-настройка, где ты указываешь, какой эффект использовать для каждого типа поверхности.

Как создать:

1. В Project window нажми правой кнопкой.
2. Выбери `Create > FPS Weapons > Surface Effect Profile`.
3. Назови asset, например `DefaultSurfaceEffects`.
4. В `HandRailGun` на компоненте `WeaponImpactSystem` перетащи этот asset в поле `Effect Profile`.

Внутри profile можно настроить:

- `Default Effect`;
- эффекты для `Metal`;
- эффекты для `Concrete`;
- эффекты для `Flesh`;
- эффекты для `Mechanical`.

Каждый effect definition может содержать:

- `Surface Type`;
- `Physics Materials`;
- `Tags`;
- `Impact Particles`;
- `Decal Prefab`;
- `Decal Offset`;
- `Lifetime`.

## Определение поверхности через Physics Material

Это предпочтительный способ.

Пример:

1. Создай Physics Material:
   - `Metal_PhysicsMaterial`
   - `Concrete_PhysicsMaterial`
   - `Flesh_PhysicsMaterial`
2. Назначь Physics Material на Collider объекта.
3. В `SurfaceEffectProfile` добавь этот material в нужный тип поверхности.

Например:

```text
Metal:
  Physics Materials:
    Metal_PhysicsMaterial
  Impact Particles:
    Metal sparks prefab

Concrete:
  Physics Materials:
    Concrete_PhysicsMaterial
  Impact Particles:
    Dust/debris prefab
```

## Определение поверхности через SurfaceIdentifier

Если Physics Material неудобен, можно повесить компонент `SurfaceIdentifier`.

Пример:

1. Выбери объект стены, врага или props.
2. Добавь компонент `SurfaceIdentifier`.
3. Выбери `Surface Type`.

Доступные типы:

- `Default`
- `Metal`
- `Concrete`
- `Flesh`
- `Mechanical`

Этот способ удобен для врагов. Например, живому монстру можно поставить `Flesh`, а роботу `Mechanical`.

## Определение поверхности через Tags

Tags поддерживаются как запасной вариант.

Пример:

1. Создай tag `Metal`.
2. Назначь его объекту.
3. В `SurfaceEffectProfile` в поле `Tags` добавь `Metal`.

Для учебного проекта это нормально, но в больших проектах лучше использовать Physics Materials или `SurfaceIdentifier`.

## Decals

Decal - это след попадания на поверхности:

- bullet hole;
- scorch mark;
- blood splat.

Как работает:

1. Raycast попадает в поверхность.
2. `WeaponImpactSystem` берет normal поверхности из `RaycastHit`.
3. Decal создается в точке попадания.
4. Decal поворачивается по normal поверхности.
5. Через `Lifetime` он возвращается в pool или отключается.

Важно: decal prefab должен быть ориентирован так, чтобы его forward-направление смотрело наружу от поверхности. Если decal появляется боком, нужно повернуть prefab.

## Shell Ejection

Shell ejection - выброс гильз.

За это отвечает `ShellEjector`.

Настройка:

1. Добавь на оружие компонент `ShellEjector`.
2. Создай `ShellEjectionPoint` около окна выброса гильз.
3. Создай prefab гильзы.
4. На prefab гильзы добавь `Rigidbody`.
5. Назначь prefab и точку выброса в `ShellEjector`.

Если гильзы пока не нужны, компонент можно не использовать.

## Object Pooling

Система использует `FpsObjectPool`.

Object pooling нужен, чтобы не создавать и не уничтожать эффекты каждый кадр. Вместо этого объект:

1. создается один раз;
2. используется;
3. выключается;
4. потом используется повторно.

Это важно для FPS, потому что выстрелов и эффектов может быть много.

Тебе обычно не нужно вручную настраивать pool. Скрипты используют его автоматически через:

```csharp
FpsObjectPool.Spawn(...)
```

## Урон по врагам

`FpsWeaponController` ищет компонент `Health` на объекте попадания или его родителях:

```csharp
Health health = hit.collider.GetComponentInParent<Health>();
```

Если `Health` найден, оружие вызывает:

```csharp
health.Change(-damage);
```

Значит, чтобы враг получал урон:

1. У врага должен быть Collider.
2. У врага или его родителя должен быть `Health`.
3. Collider врага должен попадать в `Hit Mask` оружия.

## Что проверить в Unity после настройки

Минимальный тест:

1. Открой `Assets/Scenes/SampleScene.unity`.
2. Выбери `player`.
3. Убедись, что `HandRailGun` находится внутри камеры игрока.
4. Нажми Play.
5. Левая кнопка мыши должна стрелять.
6. Оружие должно быть видно поверх мира.
7. Подойди к стене: оружие не должно визуально проваливаться в стену.
8. Попади во врага с `Health`: здоровье должно уменьшиться.

## Частые проблемы

### Оружие не видно

Проверь:

- есть ли layer `Weapon`;
- не выключен ли объект `HandRailGun`;
- есть ли `FpsWeaponViewSetup`;
- есть ли `Main Camera`;
- создана ли в Play Mode дочерняя `Weapon Camera`;
- не поставлен ли слишком маленький/большой scale оружия.

### Оружие стреляет два раза

Скорее всего, одновременно работают старая и новая системы.

Проверь:

- старый `Shoot` должен быть disabled;
- у `FpsWeaponViewSetup` должен быть включен `Disable Legacy Shoot`.

### Выстрелы не попадают

Проверь:

- `Hit Mask`;
- есть ли Collider у цели;
- не находится ли цель на исключенном layer;
- не стоит ли `Is Trigger`, если ты ожидаешь обычный Raycast-hit.

### Враг не получает урон

Проверь:

- есть ли `Health` на враге или родителе;
- попадает ли Raycast в Collider врага;
- не находится ли Collider врага на layer, исключенном из `Hit Mask`;
- не слишком ли маленький `Damage`.

### Нет muzzle flash

Проверь:

- назначен ли `Muzzle Flash`;
- отключен ли `Play On Awake`;
- вызывается ли `Play` при выстреле;
- находится ли эффект на layer `Weapon`, если он должен быть виден weapon camera.

### Нет impact effects

Проверь:

- назначен ли `Effect Profile` в `WeaponImpactSystem`;
- есть ли нужный effect в profile;
- назначен ли `Impact Particles`;
- есть ли Physics Material, tag или `SurfaceIdentifier` у цели.

## Рекомендуемый порядок развития системы

Для учебного проекта лучше развивать систему постепенно:

1. Добиться стабильной стрельбы raycast-уроном.
2. Настроить Weapon Camera и layer `Weapon`.
3. Добавить отдачу.
4. Добавить muzzle flash.
5. Добавить impact effects.
6. Добавить tracers.
7. Добавить shell ejection.
8. Добавить reload/ammo.
9. Вынести характеристики оружия в ScriptableObject.
10. Добавить несколько видов оружия.

## Важные Unity-концепции

### Raycast

`Raycast` - это невидимый луч. Он проверяет, что находится по направлению взгляда игрока.

В FPS это удобно, потому что попадание происходит мгновенно.

### LayerMask

`LayerMask` говорит Raycast или камере, какие слои учитывать.

В этой системе:

- `Main Camera` не рисует `Weapon`;
- `Weapon Camera` рисует только `Weapon`;
- `FpsWeaponController` не стреляет по `Weapon`.

### Prefab

Prefab - это шаблон объекта. Если настроить `HandRailGun.prefab`, изменения могут применяться ко всем экземплярам этого оружия.

### Inspector

`[SerializeField]` позволяет видеть private-поля в Inspector. Это хороший стиль: поле можно настроить в Unity, но другие скрипты не получают лишний public-доступ.

## Минимальная настройка нового оружия

Если хочешь создать новое FPS-оружие:

1. Создай prefab модели оружия.
2. Добавь пустой child `ShootPoint` около дула.
3. Добавь на root оружия:
   - `FpsWeaponViewSetup`
   - `FpsWeaponController`
   - `WeaponRecoil`
   - `WeaponImpactSystem`
4. В `FpsWeaponController` настрой:
   - `Fire Mode`
   - `Damage`
   - `Range`
   - `Rounds Per Minute`
   - `Spread Angle`
5. В `WeaponRecoil` настрой силу отдачи.
6. Перетащи оружие внутрь `Main Camera` игрока.
7. Нажми Play и проверь стрельбу.

## Минимальные рабочие значения

Для автомата:

```text
Fire Mode: Automatic
Damage: 25-35
Range: 150-250
Rounds Per Minute: 550-750
Spread Angle: 0.2-0.6
Vertical Recoil: 1.5-3
Horizontal Recoil: 0.4-1
```

Для пистолета:

```text
Fire Mode: SemiAuto
Damage: 35-50
Range: 80-150
Rounds Per Minute: 250-400
Spread Angle: 0.1-0.35
Vertical Recoil: 2-4
Horizontal Recoil: 0.3-0.8
```

## Что пока можно улучшить

Текущая система уже подходит для учебного FPS, но дальше можно добавить:

- ammo;
- reload;
- weapon switching;
- aim down sights;
- spread growth при зажатой стрельбе;
- recoil pattern;
- audio;
- animation events;
- ScriptableObject для характеристик оружия;
- разные profiles для разных видов оружия.
