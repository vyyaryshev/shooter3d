# Shooter3D Auto Save

Локальный embedded UPM-пакет для Unity 6000.2. Подхватывается Unity автоматически
из `Packages/com.shooter3d.editor-autosave`; загрузка из стороннего реестра не нужна.

В этом проекте автосохранение включено с интервалом **10 минут (600 секунд)**.
Настройки хранятся в `ProjectSettings/Shooter3DAutoSave.json` и доступны в
**Edit → Project Settings → Shooter3D → Auto Save**.
Ручная проверка: **Tools → Shooter3D → Auto Save → Save Now**.

Сохраняются изменённые открытые сцены в существующие файлы и изменённые ассеты.
Новой сцене сначала нужен обычный Save As. В Prefab Mode используется штатный
переключатель Unity Auto Save (в этом проекте уже разрешён).
Во время Play Mode, импорта, компиляции, сборки и перетаскивания мышью сохранение
откладывается до возвращения Editor в свободное состояние. В batch mode таймер отключён.
Перекомпиляция скриптов не сбрасывает срок следующего сохранения.

Это сохранение поверх текущих файлов, а не история резервных копий и не git-коммит.
Пакет не сохраняет игровой runtime в сцену и не запускает Play Mode.

## Проверка в Editor

Измени позицию тестового объекта в уже сохранённой сцене, затем нажми Save Now.
Звёздочка несохранённой сцены должна исчезнуть, а Console показать `[Auto Save]`.
Повтори изменение и оставь Editor в режиме редактирования на 10 минут:
должно выполниться такое же сохранение по таймеру.

Используются штатные API Unity:
- https://docs.unity3d.com/6000.2/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveScene.html
- https://docs.unity3d.com/6000.2/Documentation/ScriptReference/AssetDatabase.SaveAssets.html
- https://docs.unity3d.com/6000.2/Documentation/Manual/upm-embed.html
