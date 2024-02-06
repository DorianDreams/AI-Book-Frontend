namespace echo17.EndlessBook.Demo03
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
    using echo17.EndlessBook;
    using UnityEngine.UI;
    using UnityEngine.Localization;
    using static System.Net.Mime.MediaTypeNames;
    using TMPro;
    using Image = UnityEngine.UI.Image;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// This demo shows one way you could implement manual page dragging in your book
    /// </summary>
    public class BookController : MonoBehaviour
	{

		// Book and Page Control
        public Camera sceneCamera;					// The scene camera used for ray casting
        public EndlessBook book;					// The book to control
        public float turnStopSpeed;					// The speed to play the page turn animation when the mouse is let go
        public bool reversePageIfNotMidway = true;  // reverse direction if not past midway point of book
        protected BoxCollider boxCollider;			// The box collider to check for mouse motions
        protected bool isMouseDown;					// Whether the mouse is currently down
        protected bool turnBookPage = true;			// Keep track whether or not the book can be turned

        // Audio Sources
        protected bool audioOn = false;				// =false so that we don't get an open sound at the beginning
        public AudioSource bookOpenSound;			// The sound to make when the book opens
        public AudioSource bookCloseSound;			// The sound to make when the book closes
        public AudioSource pageTurnSound;			// The sounds for each of the page components' turn
        public AudioSource pagesFlippingSound;		// The sound to make when multiple pages are turning


		
		[Header("Page Objects")]
		public GameObject RenderPages;				// Page Cameras to enable/disable after rendering
        public GameObject textP0;
		public GameObject DownArrow;
		public GameObject textP1;
		public GameObject textP2;
		public LocalizedString drawPictureText;
        public GameObject DownArrow2;
        public GameObject imageP2;
        public GameObject textP3;
        public GameObject imageP4;
        public GameObject textP5;
        public GameObject imageP6;



		// Delete after testing
		private bool endBook = false;


        void OnStartStory()
        {

            book.SetState(EndlessBook.StateEnum.OpenMiddle, onCompleted: StartStory);
			Metadata.Instance.currentTextPage = 1;
            turnBookPage = false;


        }

        private StateChangedDelegate StartStory;




        [SerializeField]
		private LocalizedString FirstPageText;

        void Awake()
		{
            // cache the box collider for faster referencing
            boxCollider = gameObject.GetComponent<BoxCollider>();
            Debug.Log(boxCollider);
			
        }

		private StateChangedDelegate OnBookOpened;
		private StateChangedDelegate OnBookClosed;

        private void Start()
        {
			Debug.Log("Start Book Controller");
            textP0.GetComponent<TypewriterEffect>().CompleteTextRevealed += OnCompleteTextRevealed;
			textP1.GetComponent<TypewriterEffect>().CompleteTextRevealed += OnCompleteTextRevealed;
			textP2.GetComponent<TypewriterEffect>().CompleteTextRevealed += OnCompleteTextRevealed;
            EventSystem.instance.StartStory += OnStartStory;
			EventSystem.instance.ChangeLocale += OnChangeLocale;
			EventSystem.instance.PublishToBook += OnPublishToBook;
			//DownArrow.GetComponent<Button>().onClick.AddListener(OnDownArrowClicked);
            //DownArrow2.GetComponent<Button>().onClick.AddListener(OnDownArrowClicked);
            StartStory = (EndlessBook.StateEnum fromState,
                                                EndlessBook.StateEnum toState,
                                                int pageNumber) =>
            {
				textP1.GetComponent<TextMeshProUGUI>().text = Metadata.Instance.selectedOpeningSentence;
            };

            OnBookOpened = (EndlessBook.StateEnum fromState,
				             EndlessBook.StateEnum toState,
						                    int pageNumber) =>
			{
				turnBookPage = false;
                
                textP0.GetComponent<TextMeshProUGUI>().text = FirstPageText.GetLocalizedString();
            };

			OnBookClosed = (EndlessBook.StateEnum fromState,
							 EndlessBook.StateEnum toState,
											int pageNumber) =>
			{
                StartCoroutine(RestartInThree());
            };
        }
		
		void OnChangeLocale()
		{
            textP0.GetComponent<TextMeshProUGUI>().text = FirstPageText.GetLocalizedString();
        }

        private void OnDownArrowClicked()
        {
            if (Metadata.singleScreenVersion)
			{
				DownArrow.SetActive(false);
				DownArrow2.SetActive(false);
				EventSystem.instance.SwitchCameraEvent();

            }
        }


		IEnumerator RestartInThree()
		{
			yield return new WaitForSeconds(3);
			SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
		}


		void OnPublishToBook(Sprite sprite, string description)
		{
			DownArrow2.SetActive(false);
			description = "\n\n... " + description + " ... ";
			textP1.GetComponent<TextMeshProUGUI>().text += description;
			textP2.SetActive(false);
			imageP2.GetComponent<Image>().sprite = sprite;
            imageP2.SetActive(true);
        }


        void OnCompleteTextRevealed()
		{
            Debug.Log("OnCompleteTextRevealed");
			switch (Metadata.Instance.currentTextPage)
			{
				case 0:
					DownArrow.SetActive(true);

					if (Metadata.singleScreenVersion)
					{
						EventSystem.instance.SwitchCameraEvent();
					}
                    
                    
					break;
				case 1:
					textP2.GetComponent<TextMeshProUGUI>().text = drawPictureText.GetLocalizedString();
                    Metadata.Instance.currentTextPage = 2;
                    break;
				case 2:
                    DownArrow2.SetActive(true);
                    if (Metadata.singleScreenVersion)
                    {
                        EventSystem.instance.SwitchCameraEvent();
                    }
                    
					Metadata.Instance.currentTextPage = 3;
                    break;
				case 3:
					turnBookPage = true;
					endBook = true;
                    break;
				case 5:
                    break;
				case 7:
                    break;

			}
        }




        /// <summary>
        /// Fired when the mouse intersects with the collider box while mouse down occurs
        /// </summary>
        void OnMouseDown()
		{
			Debug.Log("OnMouseDown");
			if (turnBookPage)
			{
				if (book.CurrentState == EndlessBook.StateEnum.ClosedFront)
				{
					book.SetState(EndlessBook.StateEnum.OpenFront, onCompleted: OnBookOpened);
					Metadata.Instance.currentTextPage = 0;
					//EventSystem.instance.OpenBookEvent();
					return;
				}

				if(endBook)
				{
                    book.SetState(EndlessBook.StateEnum.ClosedBack, onCompleted: OnBookClosed);
                    return;
                }

				if (book.CurrentState == EndlessBook.StateEnum.OpenFront)
				{

					book.SetState(EndlessBook.StateEnum.OpenMiddle);
					return;
				}


				if (book.IsTurningPages || book.IsDraggingPage || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
				{
					// exit if already turning
					return;
				}

				// get the normalized time based on the mouse position
				var normalizedTime = GetNormalizedTime();

				// calculate the direction of the page turn based on the mouse position
				var direction = normalizedTime > 0.5f ? Page.TurnDirectionEnum.TurnForward : Page.TurnDirectionEnum.TurnBackward;

				// tell the book to start turning a page manually
				book.TurnPageDragStart(direction);

				// the mosue is now currently down
				isMouseDown = true;
			}

        }


		/// <summary>
	    /// Fired when the mouse intersects with the collider box while dragging
	    /// </summary>
		void OnMouseDrag()
		{
			if (book.CurrentState == EndlessBook.StateEnum.ClosedFront || book.IsTurningPages || !book.IsDraggingPage || !isMouseDown || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
			{
				// if not turning or the mouse is not down, then exit
				return;
			}

			// get the normalized time based on the mouse position
			var normalizedTime = GetNormalizedTime();

            // tell the book to move the manual page drag to the normalized time
            book.TurnPageDrag(normalizedTime);
		}

		/// <summary>
	    /// Fired when the mouse intersects with the collider and the mouse up event occurs
	    /// </summary>
		void OnMouseUp()
		{
			if (book.CurrentState == EndlessBook.StateEnum.ClosedFront || book.IsTurningPages || !book.IsDraggingPage || UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
			{
				// if not turning then exit
				return;
			}

			// tell the book to stop manual turning.
			// if we have reversePageIfNotMidway on, then we look to see if we have turned past the midway point.
			// if not, we reverse the page.
			book.TurnPageDragStop(turnStopSpeed, PageTurnCompleted, reverse: reversePageIfNotMidway ? (book.TurnPageDragNormalizedTime < 0.5f) : false);
            if (book.TurnPageDragNormalizedTime >= 0.5f)
			{
                //pageTurnSound.Play();
            }
            

            // mouse is no longer down, so we can turn a new page if the animation is also completed
            isMouseDown = false;
			
        }

		/// Calculates the normalized time based on the mouse position
		protected virtual float GetNormalizedTime()
		{
			// get the ray from the camera to the screen
			var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			// cast a ray and see where it hits
			if (Physics.Raycast(ray, out hit))
			{
				// return the position of the ray cast in terms of the normalized position of the collider box
				return (hit.point.x + (boxCollider.size.x / 2.0f)) / boxCollider.size.x;
			}

			// if we didn't hit the collider, then check to see if we are on the
			// left or right side of the screen and calculate the normalized time appropriately
			var viewportPoint = sceneCamera.ScreenToViewportPoint(Input.mousePosition);
			return (viewportPoint.x >= 0.5f) ? 1 : 0;
		}

		/// Called when the page completes its manual turn
		protected virtual void PageTurnCompleted(int leftPageNumber, int rightPageNumber)
		{
            //isTurning = false;
			Metadata.Instance.currentTextPage = leftPageNumber;
            DebugCurrentState();
			
        }
        void DebugCurrentState()
        {
            Debug.Log("CurrentState: " + book.CurrentState);
            Debug.Log("CurrentPageNumber: " + book.CurrentPageNumber);
        }
    }


}
