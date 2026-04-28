using System.Collections.Generic;
using Game.Core.DI;
using Game.Core.Events;
using Game.Player;
using Game.Player.Inventory.Events;
using InventoryItemAddedEvent = Game.Player.Inventory.Events.ItemAddedEvent;
using ItemConsumedEvent = Game.Player.Inventory.Events.ItemConsumedEvent;
using UnityEngine.SceneManagement;
using UnityEngine;


namespace Game.Tutorial
{
    public class TutorialManager : MonoBehaviour, ITutorialManager
    {
        [Header("Data")]
        [SerializeField] private string tutorialResourcePath = "Tutorial/TutorialData";

        [Header("Debug")]
        [SerializeField] private bool debugLogs;
        [SerializeField] private bool tutorialOnStart = false;

        private IEventBus _eventBus;
        private ISaveLoadService _saveLoadService;
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
        private float _lastPublishedStepProgress = -1f;

        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public int CurrentStepIndex { get; private set; } = -1;
        public float CurrentStepProgress => ComputeCurrentStepProgress();

        /// <summary>Called by GameServiceBootstrapper after registration.</summary>
        public void Initialize(IEventBus eventBus, ISaveLoadService saveLoadService,
            PlayerControllerRefactored player, CinemachinePlayerCamera playerCamera)
        {
            _eventBus        = eventBus;
            _saveLoadService = saveLoadService;
            _player          = player;
            _playerCamera    = playerCamera;

            _tutorialData = Resources.Load<TutorialData>(tutorialResourcePath);
            if (_tutorialData == null)
            {
                Debug.LogError($"[TutorialManager] TutorialData not found at Resources/{tutorialResourcePath}. Tutorial disabled.");
            }

            SubscribeToEvents();
            StartTutorial();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (_eventBus == null)
            {
                Debug.LogError("[TutorialManager] Cannot subscribe to events because IEventBus reference is missing.");
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
            _eventBus.Subscribe<ItemConsumedEvent>(OnItemConsumed);
            _eventBus.Subscribe<CanteenRefilledTutorialEvent>(OnCanteenRefilled);
            _eventBus.Subscribe<AssessmentTerminalUsedTutorialEvent>(OnAssessmentTerminalUsed);
            _eventBus.Subscribe<CampfireUsedTutorialEvent>(OnCampfireUsed);
            _eventBus.Subscribe<LighthouseUsedTutorialEvent>(OnLighthouseUsed);
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
            _eventBus.Unsubscribe<ItemConsumedEvent>(OnItemConsumed);
            _eventBus.Unsubscribe<CanteenRefilledTutorialEvent>(OnCanteenRefilled);
            _eventBus.Unsubscribe<AssessmentTerminalUsedTutorialEvent>(OnAssessmentTerminalUsed);
            _eventBus.Unsubscribe<CampfireUsedTutorialEvent>(OnCampfireUsed);
            _eventBus.Unsubscribe<LighthouseUsedTutorialEvent>(OnLighthouseUsed);
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
            PublishStepProgressIfNeeded();
            TryCompleteByPolling(step);
        }

        public void StartTutorial()
        {
            if (!tutorialOnStart)
            {
                return;
            }
            EnsureReferences();
            EnsureSaveData();

            var tutorialSave = GetOrCreateTutorialSaveData();
            if (tutorialSave != null && tutorialSave.isCompleted)
            {
                IsCompleted = true;
                Debug.LogWarning("[TutorialManager] Tutorial already completed according to save data. Marking as completed without starting.");
                return;
            }

            int startStepIndex = GetStartStepIndexFromSave(tutorialSave);

            IsActive = true;
            IsCompleted = false;
            CurrentStepIndex = -1;
            _isWaitingForGate = false;
            _waitingStepIndex = -1;

            _eventBus?.Publish(new TutorialStartedEvent(_tutorialData.tutorialId));
            ActivateStepOrWait(startStepIndex);

            if (debugLogs)
            {
                Debug.Log($"[TutorialManager] Tutorial started from step {startStepIndex}.");
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

        public void SyncToSaveData(TutorialSaveData tutorialSaveData)
        {
            if (tutorialSaveData == null)
            {
                return;
            }

            EnsureReferences();

            var runtimeSave = GetOrCreateTutorialSaveData();
            int highestCompletedStep = Mathf.Max(tutorialSaveData.lastCompletedStep, CurrentStepIndex);
            if (runtimeSave != null)
            {
                highestCompletedStep = Mathf.Max(highestCompletedStep, runtimeSave.lastCompletedStep);
            }

            tutorialSaveData.lastCompletedStep = highestCompletedStep;
            tutorialSaveData.isCompleted = tutorialSaveData.isCompleted || IsCompleted || (runtimeSave?.isCompleted ?? false);
        }

        private int GetStartStepIndexFromSave(TutorialSaveData tutorialSave)
        {
            if (_tutorialData == null || _tutorialData.steps == null || _tutorialData.steps.Count == 0)
            {
                return 0;
            }

            if (tutorialSave == null)
            {
                return 0;
            }

            return Mathf.Clamp(tutorialSave.lastCompletedStep, 0, _tutorialData.steps.Count - 1);
        }

        private void EnsureReferences()
        {
            if (_eventBus == null)
            {
                Debug.LogError("[TutorialManager] Missing IEventBus. Ensure GameServiceBootstrapper initializes TutorialManager.");
            }

            if (_saveLoadService == null)
            {
                Debug.LogError("[TutorialManager] Missing ISaveLoadService. Ensure GameServiceBootstrapper initializes TutorialManager.");
            }

            _player ??= ServiceContainer.Instance.TryGet<PlayerControllerRefactored>();
            _playerCamera ??= ServiceContainer.Instance.TryGet<CinemachinePlayerCamera>();
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
                _lastPublishedStepProgress = -1f;
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
            PublishStepProgressIfNeeded(true);
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
                    return _hasSeenInteractableInRange;
                case TutorialStepType.OpenInventory:
                case TutorialStepType.OpenContextMenu:
                case TutorialStepType.CompleteCraft:
                    return _hasObtainedFirstItem;
                default:
                    return true;
            }
        }

        public void CompleteCurrentStep()
        {
            if (!IsActive || IsCompleted)
            {
                return;
            }

            int completedIndex = CurrentStepIndex;
            PublishStepProgress(CurrentStepProgress, true);
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
            PublishStepProgress(1f, true);

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

        private float ComputeCurrentStepProgress()
        {
            if (!IsActive || IsCompleted || _isWaitingForGate)
            {
                return 0f;
            }

            var step = GetCurrentStep();
            if (step == null)
            {
                return 0f;
            }

            switch (step.completionType)
            {
                case TutorialStepType.AutoAdvance:
                    return Mathf.Clamp01(_stepTimer / Mathf.Max(0.01f, step.completionThreshold));
                case TutorialStepType.WalkDistance:
                    return Mathf.Clamp01(_walkDistance / Mathf.Max(0.01f, step.completionThreshold));
                case TutorialStepType.LookAround:
                    return Mathf.Clamp01(_lookAngle / Mathf.Max(0.01f, step.completionThreshold));
                case TutorialStepType.Jump:
                    return Mathf.Clamp01(_jumpCount / Mathf.Max(1f, Mathf.RoundToInt(step.completionThreshold)));
                case TutorialStepType.Sprint:
                    return Mathf.Clamp01(_sprintDuration / Mathf.Max(0.01f, step.completionThreshold));
                default:
                    return 0f;
            }
        }

        private void PublishStepProgressIfNeeded(bool force = false)
        {
            PublishStepProgress(CurrentStepProgress, force);
        }

        private void PublishStepProgress(float normalizedProgress, bool force = false)
        {
            if (_eventBus == null || _tutorialData == null || CurrentStepIndex < 0)
            {
                return;
            }

            float clamped = Mathf.Clamp01(normalizedProgress);
            if (!force && Mathf.Abs(clamped - _lastPublishedStepProgress) < 0.001f)
            {
                return;
            }

            _lastPublishedStepProgress = clamped;
            _eventBus.Publish(new TutorialStepProgressChangedEvent(_tutorialData.tutorialId, CurrentStepIndex, clamped));
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

            //Debug.Log($"[TutorialManager] Hold interact completed event received for {evt.Source.name}");
            var step = GetCurrentStep();
            if (step != null && (step.completionType == TutorialStepType.PressInteract || step.completionType == TutorialStepType.HoldInteract))
            {
                //Debug.Log($"[TutorialManager] Hold interact completed event received and Step is valid for completion. Completing step {CurrentStepIndex}.");
                CompleteCurrentStep();
            }
        }

        private void OnJumpExecuted(JumpExecutedEvent evt)
        {
            _jumpCount++;
        }

        private void OnItemConsumed(ItemConsumedEvent evt)
        {
            if (!IsActive || IsCompleted) return;
            var step = GetCurrentStep();
            if (step == null) return;

            switch (step.completionType)
            {
                case TutorialStepType.ConsumeItem:
                    CompleteCurrentStep();
                    break;
                case TutorialStepType.ConsumeFood:
                    if (evt.Item is not CanteenItem)
                        CompleteCurrentStep();
                    break;
                case TutorialStepType.DrinkFromCanteen:
                    if (evt.Item is CanteenItem)
                        CompleteCurrentStep();
                    break;
            }
        }

        private void OnCanteenRefilled(CanteenRefilledTutorialEvent evt)
        {
            if (!IsActive || IsCompleted) return;
            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.RefillCanteen)
                CompleteCurrentStep();
        }

        private void OnAssessmentTerminalUsed(AssessmentTerminalUsedTutorialEvent evt)
        {
            if (!IsActive || IsCompleted) return;
            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.InteractTerminal)
                CompleteCurrentStep();
        }

        private void OnCampfireUsed(CampfireUsedTutorialEvent evt)
        {
            if (!IsActive || IsCompleted) return;
            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.InteractCampfire)
                CompleteCurrentStep();
        }

        private void OnLighthouseUsed(LighthouseUsedTutorialEvent evt)
        {
            if (!IsActive || IsCompleted) return;
            var step = GetCurrentStep();
            if (step != null && step.completionType == TutorialStepType.InteractLighthouse)
                CompleteCurrentStep();
        }

    }
}
