using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Stagger.Boss
{
    /// <summary>
    /// Manages the visual thread system connecting boss limbs to the ceiling.
    /// Handles thread breaking QTE and limb disabling.
    /// Implements Observer pattern through UnityEvents.
    /// </summary>
    public class ThreadSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BossData _bossData;
        [SerializeField] private Transform _ceilingAnchor; // Point where threads connect to ceiling
        
        [Header("Thread Visuals")]
        [SerializeField] private Color _intactThreadColor = Color.white;
        [SerializeField] private Color _brokenThreadColor = Color.gray;
        [SerializeField] private float _threadWidth = 0.1f;
        [SerializeField] private Material _threadMaterial;
        
        [Header("Thread Break")]
        [SerializeField] private float _qteWindowDuration = 2f; // Time to parry during QTE
        [SerializeField] private KeyCode _qteKey = KeyCode.Space; // Key to press for QTE
        
        [Header("Events - Observer Pattern")]
        [Tooltip("Raised when a thread is broken")]
        public UnityEvent<int> OnThreadBroken;
        
        [Tooltip("Raised when all threads are broken")]
        public UnityEvent OnAllThreadsBroken;
        
        [Tooltip("Raised when QTE succeeds")]
        public UnityEvent OnQTESuccess;
        
        [Tooltip("Raised when QTE fails")]
        public UnityEvent OnQTEFailed;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebug = true;

        private List<LineRenderer> _threadRenderers = new List<LineRenderer>();
        private List<bool> _threadStates = new List<bool>(); // true = intact, false = broken
        private int _brokenThreadCount = 0;
        private bool _isQTEActive = false;
        private float _qteStartTime;
        private int _currentQTEThreadIndex = -1;

        // Properties
        public int TotalThreads => _threadStates.Count;
        public int BrokenThreads => _brokenThreadCount;
        public int IntactThreads => TotalThreads - _brokenThreadCount;
        public bool AllThreadsBroken => _brokenThreadCount >= TotalThreads;
        public bool IsQTEActive => _isQTEActive;

        /// <summary>
        /// Initialize the thread system with boss data.
        /// </summary>
        public void Initialize(BossData bossData, Transform ceilingAnchor = null)
        {
            _bossData = bossData;
            
            if (ceilingAnchor != null)
                _ceilingAnchor = ceilingAnchor;
            
            // Clear existing threads
            foreach (LineRenderer lr in _threadRenderers)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _threadRenderers.Clear();
            _threadStates.Clear();
            _brokenThreadCount = 0;

            // Create threads for each configured thread point
            for (int i = 0; i < _bossData.ThreadCount; i++)
            {
                CreateThread(i);
                _threadStates.Add(true); // All threads start intact
            }

            Debug.Log($"[ThreadSystem] Initialized {_bossData.ThreadCount} threads");
        }

        /// <summary>
        /// Create a visual thread LineRenderer.
        /// </summary>
        private void CreateThread(int index)
        {
            GameObject threadObj = new GameObject($"Thread_{index}");
            threadObj.transform.SetParent(transform);
            
            LineRenderer lr = threadObj.AddComponent<LineRenderer>();
            
            // Configure LineRenderer
            lr.startWidth = _threadWidth;
            lr.endWidth = _threadWidth;
            lr.material = _threadMaterial != null ? _threadMaterial : new Material(Shader.Find("Sprites/Default"));
            lr.startColor = _intactThreadColor;
            lr.endColor = _intactThreadColor;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            
            _threadRenderers.Add(lr);
        }

        private void Update()
        {
            UpdateThreadPositions();
            
            if (_isQTEActive)
            {
                UpdateQTE();
            }
        }

        /// <summary>
        /// Update thread visual positions every frame.
        /// </summary>
        private void UpdateThreadPositions()
        {
            for (int i = 0; i < _threadRenderers.Count; i++)
            {
                if (_threadRenderers[i] == null) continue;
                
                // Get attachment point from boss data
                Vector2 attachmentOffset = i < _bossData.Threads.Count 
                    ? _bossData.Threads[i].AttachmentPoint 
                    : new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f));
                
                Vector3 bossAttachment = transform.position + (Vector3)attachmentOffset;
                Vector3 ceilingPoint = _ceilingAnchor != null 
                    ? _ceilingAnchor.position 
                    : transform.position + Vector3.up * 5f;
                
                Debug.DrawLine(ceilingPoint, bossAttachment, Color.yellow);
                
                // Set line positions
                _threadRenderers[i].SetPosition(0, ceilingPoint);
                _threadRenderers[i].SetPosition(1, bossAttachment);
                
                // Update color based on state
                Color color = _threadStates[i] ? _intactThreadColor : _brokenThreadColor;
                _threadRenderers[i].startColor = color;
                _threadRenderers[i].endColor = color;
                
                // Hide if broken
                _threadRenderers[i].enabled = _threadStates[i];
            }
            
            
            
        }

        /// <summary>
        /// Start a thread break QTE for a specific thread.
        /// </summary>
        public void StartThreadBreakQTE(int threadIndex)
        {
            if (threadIndex < 0 || threadIndex >= _threadStates.Count)
            {
                Debug.LogWarning($"[ThreadSystem] Invalid thread index: {threadIndex}");
                return;
            }

            if (!_threadStates[threadIndex])
            {
                Debug.LogWarning($"[ThreadSystem] Thread {threadIndex} already broken");
                return;
            }

            _isQTEActive = true;
            _qteStartTime = Time.time;
            _currentQTEThreadIndex = threadIndex;

            Debug.Log($"[ThreadSystem] <color=yellow>THREAD BREAK QTE STARTED!</color> Thread {threadIndex} - Press {_qteKey}!");
            
            // Make thread flash or pulse to indicate QTE
            StartCoroutine(FlashThread(threadIndex));
        }

        /// <summary>
        /// Update QTE logic.
        /// </summary>
        private void UpdateQTE()
        {
            float elapsedTime = Time.time - _qteStartTime;
            
            // Check for input
            if (Input.GetKeyDown(_qteKey))
            {
                // Success!
                QTESuccess();
            }
            // Check for timeout
            else if (elapsedTime >= _qteWindowDuration)
            {
                // Failed!
                QTEFailed();
            }
        }

        /// <summary>
        /// Called when QTE is successful.
        /// </summary>
        private void QTESuccess()
        {
            Debug.Log($"[ThreadSystem] <color=green>QTE SUCCESS!</color> Thread {_currentQTEThreadIndex} severed!");
    
            BreakThread(_currentQTEThreadIndex);
            _isQTEActive = false;
            _currentQTEThreadIndex = -1;
    
            // Raise event (Observer pattern)
            OnQTESuccess?.Invoke();
    
            // Play thread break sound
            if (_bossData != null && _bossData.ThreadBreakSound != null)
            {
                AudioSource.PlayClipAtPoint(_bossData.ThreadBreakSound, transform.position);
            }
    
            // REMOVE BOSS INVULNERABILITY - boss can take damage again
            BossHealth bossHealth = GetComponent<BossHealth>();
            if (bossHealth != null)
            {
                bossHealth.SetInvulnerable(false);
                Debug.Log("[ThreadSystem] Boss is now VULNERABLE - combat resumes!");
            }
        }

        /// <summary>
        /// Called when QTE times out.
        /// </summary>
        private void QTEFailed()
        {
            Debug.Log($"[ThreadSystem] <color=red>QTE FAILED!</color> Thread {_currentQTEThreadIndex} still intact");
    
            _isQTEActive = false;
            _currentQTEThreadIndex = -1;
    
            // Raise event (Observer pattern)
            OnQTEFailed?.Invoke();
    
            // KEEP BOSS INVULNERABLE - player must try again
            // Boss will retry the thread break attack
            Debug.Log("[ThreadSystem] Boss remains invulnerable - retrying thread break attack!");
        }

        /// <summary>
        /// Break a specific thread.
        /// </summary>
        public void BreakThread(int threadIndex)
        {
            if (threadIndex < 0 || threadIndex >= _threadStates.Count)
            {
                Debug.LogWarning($"[ThreadSystem] Invalid thread index: {threadIndex}");
                return;
            }

            if (!_threadStates[threadIndex])
            {
                Debug.LogWarning($"[ThreadSystem] Thread {threadIndex} already broken");
                return;
            }

            _threadStates[threadIndex] = false;
            _brokenThreadCount++;

            Debug.Log($"[ThreadSystem] Thread {threadIndex} broken! ({_brokenThreadCount}/{TotalThreads})");

            // Raise event (Observer pattern)
            OnThreadBroken?.Invoke(threadIndex);

            // Check if all threads are broken
            if (AllThreadsBroken)
            {
                Debug.Log($"[ThreadSystem] <color=yellow>ALL THREADS BROKEN! EXECUTION AVAILABLE!</color>");
                OnAllThreadsBroken?.Invoke();
            }

            // Disable linked attacks
            DisableLimbAttacks(threadIndex);
        }

        /// <summary>
        /// Disable attacks linked to this thread/limb.
        /// </summary>
        private void DisableLimbAttacks(int threadIndex)
        {
            if (_bossData == null || threadIndex >= _bossData.Threads.Count) return;

            ThreadData thread = _bossData.Threads[threadIndex];
            Debug.Log($"[ThreadSystem] Limb '{thread.LimbName}' disabled - attacks linked to this thread are now unavailable");
            
            // Boss AI will check thread states when selecting attacks
        }

        /// <summary>
        /// Check if a specific thread is intact.
        /// </summary>
        public bool IsThreadIntact(int threadIndex)
        {
            if (threadIndex < 0 || threadIndex >= _threadStates.Count)
                return false;
            
            return _threadStates[threadIndex];
        }

        /// <summary>
        /// Flash a thread to indicate QTE.
        /// </summary>
        private IEnumerator FlashThread(int threadIndex)
        {
            if (threadIndex < 0 || threadIndex >= _threadRenderers.Count) yield break;
            
            LineRenderer lr = _threadRenderers[threadIndex];
            float flashDuration = _qteWindowDuration;
            float flashSpeed = 5f;
            float elapsed = 0f;

            while (elapsed < flashDuration && _isQTEActive)
            {
                float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
                Color flashColor = Color.Lerp(_intactThreadColor, Color.yellow, t);
                
                lr.startColor = flashColor;
                lr.endColor = flashColor;
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Reset color
            if (lr != null)
            {
                lr.startColor = _intactThreadColor;
                lr.endColor = _intactThreadColor;
            }
        }

        // Debug commands
        [ContextMenu("Debug: Break Random Thread")]
        private void DebugBreakRandomThread()
        {
            List<int> intactThreads = new List<int>();
            for (int i = 0; i < _threadStates.Count; i++)
            {
                if (_threadStates[i]) intactThreads.Add(i);
            }

            if (intactThreads.Count > 0)
            {
                int randomIndex = intactThreads[Random.Range(0, intactThreads.Count)];
                BreakThread(randomIndex);
            }
        }

        [ContextMenu("Debug: Start QTE for Thread 0")]
        private void DebugStartQTE()
        {
            StartThreadBreakQTE(0);
        }
    }
}