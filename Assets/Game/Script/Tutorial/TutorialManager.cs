using System.Collections.Generic;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player;
using Game.Player.Inventory.Events;
using InventoryItemAddedEvent = Game.Player.Inventory.Events.ItemAddedEvent;
using UnityEngine;

namespace Game.Tutorial
{
    public class TutorialManager : MonoBehaviour, ITutorialManager
    {
        [Header("Data")]
        [SerializeField] private string tutorialResourcePath = "Tutorial/TutorialData";

        [Header("Debug")]
        [SerializeField] private bool debugLogs;

        private IEventBus _eventBus;
        private SaveLoadService _saveLoadService;
        private PlayerControllerRefactored _player;
        private CinemachinePlayerCamera _playerCamera;

        private TutorialData _tutorialData;
        private TutorialSaveData _runtimeTutorialSaveData;
        private bool _hasSeenInteractableInRange;
        private bool _hasObtainedFirstItem;
        private bool _isWaitingForGate;
        private int _waitingStepIndex = -1;

        private float _stepTimer;
        private float _walkDistance;
        private float _lookAngle;
        private float _sprintDuration;
        private int _jumpCount;
        private Vector3 _lastPlayerPosition;
        private float _lastCameraYaw;

        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;

        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(IEventBus eventBus, SaveLoadService saveLoadService,
            PlayerControllerRefactored player, CinemachinePlayerCamera playerCamera)
        {
            _eventBus        = eventBus;
            _saveLoadService = saveLoadService;
            _player          = player;
            _playerCamera    = playerCamera;

            _tutorialData = Resources.Load<TutorialData>(tutorialResourcePath);
            if (_tutorialData == null)
            {
                _tutorialData = BuildFallbackTutorialData();
                if (debugLogs)
                    Debug.LogWarning("[TutorialManager] TutorialData resource was not found. Using fallback in-code steps.");
            }
        }

        private void Start()
        {
            SubscribeToEvents();
            StartTutorial();
        }

        private void OnEnable()
        {
            // Event subscriptions are now handled in Start() after EventBus is initialized
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Subscribe<InventoryItemAddedEvent>(OnItemAdded);
            _eventBus.Subscribe<ItemInRangeChangedEvent>(OnItemInRangeChanged);
            _eventBus.Subscribe<PanelOpenedEvent>(OnPanelOpened);
            _eventBus.Subscribe<ContextMenuOpenedEvent>(OnContextMenuOpened);
            _eventBus.Subscribe<CraftingCompletedEvent>(OnCraftCompleted);
            _eventBus.Subscribe<HoldInteractStartedEvent>(OnHoldInteractStarted);
            _eventBus.Subscribe<HoldInteractCompletedEvent>(OnHoldInteractCompleted);
            _eventBus.Subscribe<JumpExecutedEvent>(OnJumpExecuted);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Unsubscribe<InventoryItemAddedEvent>(OnItemAdded);
            _eventBus.Unsubscribe<ItemInRangeChangedEvent>(OnItemInRangeChanged);
            _eventBus.Unsubscribe<PanelOpenedEvent>(OnPanelOpened);
            _eventBus.Unsubscribe<ContextMenuOpenedEvent>(OnContextMenuOpened);
            _eventBus.Unsubscribe<CraftingCompletedEvent>(OnCraftCompleted);
            _eventBus.Unsubscribe<HoldInteractStartedEvent>(OnHoldInteractStarted);
            _eventBus.Unsubscribe<HoldInteractCompletedEvent>(OnHoldInteractCompleted);
            _eventBus.Unsubscribe<JumpExecutedEvent>(OnJumpExecuted);
        }

        private void Update()
        {
            if (!IsActive || IsCompleted || _tutorialData == null)
            {
                return;
            }

            if (_isWaitingForGate)
            {
                TryActivateWaitingStep();
                return;
            }

            var step = GetCurrentStep();
            if (step == null)
            {
                return;
            }

            _stepTimer += Time.deltaTime;
            UpdatePollingProgress(step);
            TryCompleteByPolling(step);
        }

        public void StartTutorial()
        {
            if (_tutorialData == null || _tutorialData.steps == null || _tutorialData.steps.Count == 0)
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[TutorialManager] Cannot start tutorial without valid data.");
                }
                return;
            }

            EnsureReferences();
            EnsureSaveData();

            var tutorialSave = GetOrCreateTutorialSaveData();
            if (tutorialSave != null && tutorialSave.isCompleted)
            {
                IsCompleted = true;
                return;
            }

            IsActive = true;
            IsCompleted = false;
            CurrentStepIndex = -1;
            _isWaitingForGate = false;
            _waitingStepIndex = -1;

            _eventBus?.Publish(new TutorialStartedEvent(_tutorialData.tutorialId));
            ActivateStepOrWait(0);

            if (debugLogs)
            {
                Debug.Log("[TutorialManager] Tutorial started.");
            }
        }

        public void SkipTutorial()
        {
            if (!IsActive)
            {
                return;
            }

            MarkCompleted(true);
            _eventBus?.Publish(new TutorialSkippedEvent(_tutorialData.tutorialId));
        }

        private void EnsureReferences()
        {
            // Dependencies are injected via Initialize(); these are no-op guards
            // in case Initialize() was not called (e.g. in tests or legacy scenes).
            _eventBus        ??= ServiceContainer.Instance.TryGet<IEventBus>();
            _saveLoadService ??= SaveLoadService.Instance ?? FindFirstObjectByType<SaveLoadService>();
            _player          ??= ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
            _playerCamera    ??= ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
        }

        private void EnsureSaveData()
        {
            GetOrCreateTutorialSaveData();
        }

        private WorldSaveData GetCurrentWorldSave()
        {
            return _saveLoadService != null ? _saveLoadService.CurrentWorldSave : null;
        }

        private TutorialSaveData GetOrCreateTutorialSaveData()
        {
            var save = GetCurrentWorldSave();
            if (save != null)
            {
                save.tutorial ??= new TutorialSaveData();
                return save.tutorial;
            }

            _runtimeTutorialSaveData ??= new TutorialSaveData();
            return _runtimeTutorialSaveData;
        }

        private TutorialStepData GetCurrentStep()
        {
            if (_tutorialData == null || CurrentStepIndex < 0 || CurrentStepIndex >= _tutorialData.steps.Count)
            {
                return null;
            }

            return _tutorialData.steps[CurrentStepIndex];
        }

        private void TryActivateWaitingStep()
        {
            if (_waitingStepIndex < 0)
            {
                _isWaitingForGate = false;
                return;
            }

            if (!IsStepGateSatisfied(_tutorialData.steps[_waitingStepIndex]))
            {
                return;
            }

            _isWaitingForGate = false;
            ActivateStep(_waitingStepIndex);
        }

        private void ActivateStepOrWait(int stepIndex)
        {
            if (stepIndex >= _tutorialData.steps.Count)
            {
                MarkCompleted(false);
                return;
            }

            if (!IsStepGateSatisfied(_tutorialData.steps[stepIndex]))
            {
                _isWaitingForGate = true;
                _waitingStepIndex = stepIndex;
                CurrentStepIndex = stepIndex;
                PublishStepChanged(stepIndex, null, true);
                return;
            }

            ActivateStep(stepIndex);
        }

        private void ActivateStep(int stepIndex)
        {
            CurrentStepIndex = stepIndex;
            _waitingStepIndex = -1;
            _isWaitingForGate = false;

            ResetStepTracking();
            PersistProgress(false);
            PublishStepChanged(stepIndex, _tutorialData.steps[stepIndex], false);
        }

        private void ResetStepTracking()
        {
            _stepTimer = 0f;
            _walkDistance = 0f;
            _lookAngle = 0f;
            _sprintDuration = 0f;
            _jumpCount = 0;

            if (_player != null)
            {
                _lastPlayerPosition = _player.transform.position;
            }

            if (_playerCamera != null)
            {
                _lastCameraYaw = _playerCamera.transform.eulerAngles.y;
            }
        }

        private void UpdatePollingProgress(TutorialStepData step)
        {
            switch (step.completionType)
            {
                case TutorialStepType.WalkDistance:
                    if (_player != null)
                    {
                        float distance = Vector3.Distance(_lastPlayerPosition, _player.transform.position);
                        _walkDistance += distance;
                        _lastPlayerPosition = _player.transform.position;
                    }
                    break;
                case TutorialStepType.LookAround:
                    if (_playerCamera != null)
                    {
                        float yaw = _playerCamera.transform.eulerAngles.y;
                        _lookAngle += Mathf.Abs(Mathf.DeltaAngle(_lastCameraYaw, yaw));
                        _lastCameraYaw = yaw;
                    }
                    break;
                case TutorialStepType.Sprint:
                    if (_player != null && _player.GetCurrentState() is RunningState)
                    {
                        _sprintDuration += Time.deltaTime;
                    }
                    break;
            }
        }

        private void TryCompleteByPolling(TutorialStepData step)
        {
            switch (step.completionType)
            {
                case TutorialStepType.AutoAdvance:
                    if (_stepTimer >= Mathf.Max(0.01f, step.completionThreshold))
                    {
                        CompleteCurrentStep();
                    }
                    break;
                case TutorialStepType.WalkDistance:
                    if (_walkDistance >= Mathf.Max(0.01f, step.completionThreshold))
                    {
                        CompleteCurrentStep();
                    }
                    break;
                case TutorialStepType.LookAround:
                    if (_lookAngle >= Mathf.Max(0.01f, step.completionThreshold))
                    {
                        CompleteCurrentStep();
                    }
                    break;
                case TutorialStepType.Jump:
                    if (_jumpCount >= Mathf.RoundToInt(Mathf.Max(1f, step.completionThreshold)))
                    {
                        CompleteCurrentStep();
                    }
                    break;
                case TutorialStepType.Sprint:
                    if (_sprintDuration >= Mathf.Max(0.01f, step.completionThreshold))
                    {
                        CompleteCurrentStep();
                    }
                    break;
            }
        }

        private bool IsStepGateSatisfied(TutorialStepData step)
        {
            if (step == null)
            {
                return false;
            }

            switch (step.completionType)
            {
                case TutorialStepType.PressInteract:
                case TutorialStepType.HoldInteract:
                    return _hasSeenInteractableInRange;
                case TutorialStepType.OpenInventory:
                case TutorialStepType.OpenContextMenu:
                case TutorialStepType.CompleteCraft:
                    return _hasObtainedFirstItem;
                default:
                    return true;
            }
        }

        private void CompleteCurrentStep()
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            int completedIndex = CurrentStepIndex;
            _eventBus?.Publish(new TutorialStepCompletedEvent(_tutorialData.tutorialId, completedIndex));

            if (completedIndex + 1 >= _tutorialData.steps.Count)
            {
                MarkCompleted(false);
                return;
            }

            ActivateStepOrWait(completedIndex + 1);
        }

        private void MarkCompleted(bool skipped)
        {
            IsCompleted = true;
            IsActive = false;
            _isWaitingForGate = false;
            _waitingStepIndex = -1;

            PersistProgress(true, true);
            _eventBus?.Publish(new TutorialCompletedEvent(_tutorialData.tutorialId));

            if (debugLogs)
            {
                Debug.Log(skipped
                    ? "[TutorialManager] Tutorial skipped and marked complete."
                    : "[TutorialManager] Tutorial completed.");
            }
        }

        private void PersistProgress(bool flushToDisk, bool completed = false)
        {
            var tutorialSave = GetOrCreateTutorialSaveData();
            if (tutorialSave == null)
            {
                return;
            }

            tutorialSave.lastCompletedStep = Mathf.Max(tutorialSave.lastCompletedStep, CurrentStepIndex);

            if (completed)
            {
                tutorialSave.isCompleted = true;
            }

            var save = GetCurrentWorldSave();
            if (save == null || !flushToDisk || _saveLoadService == null)
            {
                return;
            }

            _saveLoadService.SaveWorld(save);
        }

        private void PublishStepChanged(int stepIndex, TutorialStepData stepData, bool waitingForGate)
        {
            _eventBus?.Publish(new TutorialStepChangedEvent(_tutorialData.tutorialId, stepIndex, stepData, waitingForGate));
        }

        private void OnItemAdded(InventoryItemAddedEvent evt)
        {
            _hasObtainedFirstItem = true;
        }

        private void OnItemInRangeChanged(ItemInRangeChangedEvent evt)
        {
            if (evt.IsInRange)
            {
                _hasSeenInteractableInRange = true;
            }
        }

        private void OnPanelOpened(PanelOpenedEvent evt)
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            var step = GetCurrentStep();
            if (step == null)
            {
                return;
            }

            if (step.completionType == TutorialStepType.OpenInventory && evt.PanelName == "Inventory")
            {
                CompleteCurrentStep();
            }
        }

        private void OnContextMenuOpened(ContextMenuOpenedEvent evt)
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.OpenContextMenu)
            {
                CompleteCurrentStep();
            }
        }

        private void OnCraftCompleted(CraftingCompletedEvent evt)
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.CompleteCraft)
            {
                CompleteCurrentStep();
            }
        }

        private void OnHoldInteractStarted(HoldInteractStartedEvent evt)
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            var step = GetCurrentStep();
            if (step != null && (step.completionType == TutorialStepType.PressInteract || step.completionType == TutorialStepType.HoldInteract))
            {
                CompleteCurrentStep();
            }
        }

        private void OnHoldInteractCompleted(HoldInteractCompletedEvent evt)
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            Debug.Log($"[TutorialManager] Hold interact completed event received for {evt.Source.name}");
            var step = GetCurrentStep();
            if (step != null && (step.completionType == TutorialStepType.PressInteract || step.completionType == TutorialStepType.HoldInteract))
            {
                Debug.Log($"[TutorialManager] Hold interact completed event received and Step is valid for completion. Completing step {CurrentStepIndex}.");
                CompleteCurrentStep();
            }
        }

        private void OnJumpExecuted(JumpExecutedEvent evt)
        {
            _jumpCount++;
        }

        private TutorialData BuildFallbackTutorialData()
        {
            var data = ScriptableObject.CreateInstance<TutorialData>();
            data.tutorialId = "main_onboarding";
            data.steps = new List<TutorialStepData>
            {
                CreateStep("welcome", "Welcome", "Welcome. Let's learn the basics.", "", TutorialStepType.AutoAdvance, 3f),
                CreateStep("move", "Move", "Use movement keys to move.", "Move", TutorialStepType.WalkDistance, 3f),
                CreateStep("look", "Camera", "Move the mouse to look around.", "Look", TutorialStepType.LookAround, 90f),
                CreateStep("jump", "Jump", "Press jump.", "Jump", TutorialStepType.Jump, 1f),
                CreateStep("sprint", "Sprint", "Hold sprint while moving.", "Sprint", TutorialStepType.Sprint, 1.5f),
                CreateStep("interact", "Interact", "Interact with a nearby object - tap or hold.", "Interact", TutorialStepType.PressInteract, 1f),
                CreateStep("inventory", "Inventory", "Open inventory and check your first item.", "Inventory", TutorialStepType.OpenInventory, 1f),
                CreateStep("context_menu", "Context Menu", "Open the item context menu.", "Right-click", TutorialStepType.OpenContextMenu, 1f),
                CreateStep("craft", "Craft", "Craft your first item.", "Craft", TutorialStepType.CompleteCraft, 1f),
                CreateStep("stats", "Survival", "Watch your hunger, stamina, and temperature.", "", TutorialStepType.AutoAdvance, 5f)
            };

            return data;
        }

        private TutorialStepData CreateStep(string id, string title, string body, string hint, TutorialStepType type, float threshold)
        {
            var step = ScriptableObject.CreateInstance<TutorialStepData>();
            step.stepId = id;
            step.title = title;
            step.instructionText = body;
            step.inputHintText = hint;
            step.completionType = type;
            step.completionThreshold = threshold;
            return step;
        }
    }
}
