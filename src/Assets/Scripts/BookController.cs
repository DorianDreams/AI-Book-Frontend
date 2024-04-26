namespace echo17.EndlessBook.Demo03
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
    using echo17.EndlessBook;
    using UnityEngine.Localization;
    using TMPro;
    using Image = UnityEngine.UI.Image;
    using UnityEngine.SceneManagement;
    using UnityEngine.Networking;
    using Newtonsoft.Json;
    using System.Text;
    using Newtonsoft.Json.Bson;




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


        public GameObject BookTitle;
		
		[Header("Page Objects")]
		public GameObject RenderPages;				// Page Cameras to enable/disable after rendering
        public GameObject textP0;
		public GameObject DownArrow;
		public GameObject textP1;
		public GameObject textP2;
		public LocalizedString drawPictureText;
        public GameObject DownArrow2;
        public GameObject imageP2;
        private byte[] imageP2bytes;
        public GameObject textP3;
        public GameObject imageP4;
        private byte[] imageP4bytes;
		public GameObject textP4;
        public GameObject textP5;
        public GameObject imageP6;
        private byte[] imageP6bytes;
		public GameObject textP6;


        [Header("Book Navigator")]
        public GameObject BookNavigator;

        public GameObject previousPage;
        public GameObject nextPage;
        public GameObject regenerateText;
        public GameObject finishBook;



		// Delete after testing
		private bool endBook = false;
		private bool bookFinished = false;
		private bool nextPageFive = false;

        private int currentTextGenPage = 1;

		private int nextBookPage = 0;

        public float turnTime = 1f;
        public float stateAnimationTime = 1f;



        void OnStartStory()
        {

            book.SetState(EndlessBook.StateEnum.OpenMiddle, onCompleted: StartStory);
			Metadata.Instance.currentTextPage = 1;
            //turnBookPage = false;


        }

        private StateChangedDelegate StartStory;






        public void GoToNextPage()
        {
            switch (book.CurrentPageNumber)
			{
                case 1:
                    book.TurnForward(turnTime,
                        onCompleted: OnBookTurnToPageCompleted,
                        onPageTurnStart: OnPageTurnStart,
                        onPageTurnEnd: OnPageTurnEnd);
                    if (currentTextGenPage == 3){
                        regenerateText.SetActive(true);
                    }
                    break;
                case 3:
                    if (nextPageFive) {
                        book.TurnForward(turnTime,
                        onCompleted: OnBookTurnToPageCompleted,
                        onPageTurnStart: OnPageTurnStart,
                        onPageTurnEnd: OnPageTurnEnd);
                        if (currentTextGenPage == 5){
                        regenerateText.SetActive(true);
                    }
                    }
                    break;
                case 5:
                    Debug.Log("End it");
                    break;
            }
            
        }

        //Todo: implement title generation
        public void OnFinishBook()
        {
            
            book.SetState(EndlessBook.StateEnum.ClosedFront);
            EventSystem.instance.DisableBookNavigatorEvent();
            EventSystem.instance.EnableOwnershipScreenEvent();
            StartCoroutine(CreateTitle());
        }

        IEnumerator CreateTitle()
    {        
        string alltext = textP1.GetComponent<TextMeshProUGUI>().text + 
                         textP3.GetComponent<TextMeshProUGUI>().text +
                         textP5.GetComponent<TextMeshProUGUI>().text;

        var sb = new StringBuilder(alltext.Length);

        foreach (char i in alltext)
            if (i != '\n' && i != '\r' && i != '\t' && i!='"')
                sb.Append(i);

        alltext = sb.ToString();


        string json = "{ \"user_input\":" + "\"" + alltext + "\"" + "}";
        Debug.Log(json);
        Debug.Log("json: " + json);
        using (UnityWebRequest request = UnityWebRequest.Post("http://127.0.0.1:8000/api/chat/titles", json, "application/json"))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
                Dictionary<string, object> returnVal = JsonConvert.DeserializeObject
                    <Dictionary<string, object>>(request.downloadHandler.text);


                string title = returnVal["generated_text"].ToString();
                var sb2 = new StringBuilder(title.Length);

                foreach (char i in title)
                    if (i!='"')
                        sb2.Append(i);
                title = sb2.ToString();
                BookTitle.GetComponent<TextMeshPro>().text = title;
                BookTitle.SetActive(true);

            }
        }
    }



        public void GoToPreviousPage()
        {
            book.TurnBackward(turnTime);
                     
                regenerateText.SetActive(false);

        }


        protected virtual void OnPageTurnStart(Page page, int pageNumberFront, int pageNumberBack, int pageNumberFirstVisible, int pageNumberLastVisible, Page.TurnDirectionEnum turnDirection)
        {
            Debug.Log("OnPageTurnStart: front [" + pageNumberFront + "] back [" + pageNumberBack + "] fv [" + pageNumberFirstVisible + "] lv [" + pageNumberLastVisible + "] dir [" + turnDirection + "]");
        }

        protected virtual void OnPageTurnEnd(Page page, int pageNumberFront, int pageNumberBack, int pageNumberFirstVisible, int pageNumberLastVisible, Page.TurnDirectionEnum turnDirection)
        {
            Debug.Log("OnPageTurnEnd: front [" + pageNumberFront + "] back [" + pageNumberBack + "] fv [" + pageNumberFirstVisible + "] lv [" + pageNumberLastVisible + "] dir [" + turnDirection + "]");
        }


        [SerializeField]
		private LocalizedString FirstPageText;

        void Awake()
		{
            // cache the box collider for faster referencing
            boxCollider = gameObject.GetComponent<BoxCollider>();
            Debug.Log(boxCollider);
			
        }

		public Vector3 screenPosition;


		private StateChangedDelegate OnBookOpened;
		private StateChangedDelegate OnBookClosed;

        private void Start()
        {

			EventSystem.instance.RestartScene += Reset;
            BookTitle.SetActive(false);

			Debug.Log("Start Book Controller");
            textP0.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(0);
			textP1.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(1);
			textP2.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(2);
			textP3.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(3);
            textP4.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(4);
            textP5.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(5);
            textP6.GetComponent<TypewriterEffect>().CompleteTextRevealed += () => OnCompleteTextRevealed(6);

            EventSystem.instance.StartStory += OnStartStory;
			EventSystem.instance.ChangeLocale += OnChangeLocale;
			//EventSystem.instance.PublishToBook += OnPublishToBook;
            EventSystem.instance.SelectImage += OnSelectImage;


            EventSystem.instance.SelectText += OnSelectText;
            EventSystem.instance.PublishNextPrompt += OnPublishNextPrompt;

            EventSystem.instance.EnableBookNavigator += OnEnableBookNavigator;
            EventSystem.instance.DisableBookNavigator += OnDisableBookNavigator;

            EventSystem.instance.GoToNextPage += GoToNextPage;
            EventSystem.instance.GoPreviousPage += GoToPreviousPage;
            StartStory = (EndlessBook.StateEnum fromState,
                                                EndlessBook.StateEnum toState,
                                                int pageNumber) =>
            {
				textP1.GetComponent<TextMeshProUGUI>().text = Metadata.Instance.currentPrompt;
            };

            OnBookOpened = (EndlessBook.StateEnum fromState,
				             EndlessBook.StateEnum toState,
						                    int pageNumber) =>
			{
				//turnBookPage = false;
                
                textP0.GetComponent<TextMeshProUGUI>().text = FirstPageText.GetLocalizedString();
            };

			OnBookClosed = (EndlessBook.StateEnum fromState,
							 EndlessBook.StateEnum toState,
											int pageNumber) =>
			{
                StartCoroutine(RestartInThree());
            };

            finishBook.SetActive(false);
        }

        void OnEnableBookNavigator(){
            BookNavigator.SetActive(true); 
        }

        void OnDisableBookNavigator(){
            BookNavigator.SetActive(false); 
        }

        
        public void OnRegenerateText(){
            switch (book.CurrentPageNumber)
			{
                case 1:
                    //CoroutineWithData cd = new CoroutineWithData(this, Request.GetFullSentences(imageP2bytes, 0.5f));
                    //yield return cd.coroutine;
                    //string completed_sentence = (string) cd.result;

                    /*
                            string comleted_sentence = "penis";
                    StartCoroutine(GetChapterStories(completed_sentence,(story_generation) =>
                    {
                            textP1.GetComponent<TextMeshProUGUI>().text = completed_sentence;
                            Metadata.Instance.currentPrompt = story_generation;
                    
                            }));*/
           
        
                break;
                case 3:
                StartCoroutine(GetChapterStories(Metadata.Instance.previousPrompt, (story_generation) =>
        {
            StartCoroutine(GetFullSentences(imageP4bytes, 0.7f, (completed_sentence) =>
            {

                    // recompute continuation + description based on Metadata.Instance.previousPrompt

                    textP3.GetComponent<TextMeshProUGUI>().text = completed_sentence;
                    StartCoroutine(GetChapterStories(textP3.GetComponent<TextMeshProUGUI>().text, (story_generation) =>
        {
            Metadata.Instance.currentPrompt = story_generation;
            }));
                    }));
            }));
                    break;
                case 5:
                // recompute continuation + description based on Metadata.Instance.previousPrompt
                StartCoroutine(GetChapterStories(Metadata.Instance.previousPrompt, (story_generation) =>
        {
            StartCoroutine(GetFullSentences(imageP6bytes, 0.7f, (completed_sentence) =>
            {

                    // recompute continuation + description based on Metadata.Instance.previousPrompt


                    textP5.GetComponent<TextMeshProUGUI>().text = completed_sentence;
                    }));
            }));
                    break;
                
        
            }
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


        public void Reset()
        {
            StartCoroutine(RestartInThree());
        }

        IEnumerator RestartInThree()
		{
			yield return new WaitForSeconds(3);
			SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
		}

        void OnSelectImage(Sprite sprite, int index, byte[] imagebytes)
        {
            switch (book.CurrentPageNumber)
            {
                case 1:
                    textP2.SetActive(false);
                    imageP2bytes = imagebytes;
                    imageP2.GetComponent<Image>().sprite = sprite;
                    imageP2.SetActive(true);
                    break;
                case 3:
                    textP4.SetActive(false);
                    imageP4bytes = imagebytes;
                    imageP4.GetComponent<Image>().sprite = sprite;
                    imageP4.SetActive(true);
                    break;
                case 5:
                    textP6.SetActive(false);
                    imageP6bytes = imagebytes;
                    imageP6.GetComponent<Image>().sprite = sprite;
                    imageP6.SetActive(true);
                    break;
            }
        }

        void OnSelectText(string completion)
        {
            switch (book.CurrentPageNumber)
            {
                case 1:
                    Metadata.Instance.currentChapter = "ch2";
                    textP1.GetComponent<TextMeshProUGUI>().text = completion;
                    break;
                case 3:
                    Metadata.Instance.currentChapter = "ch3";
                    textP3.GetComponent<TextMeshProUGUI>().text = completion;
                    turnBookPage = true;
                    nextPageFive = true;
                    break;
                case 5:
                    textP5.GetComponent<TextMeshProUGUI>().text = completion;
                    turnBookPage = true;
                    bookFinished = true;
                    finishBook.SetActive(true);
                    break;
            }
        }

        void OnPublishNextPrompt(string prompt)
        {
            switch (book.CurrentPageNumber)
            {
                case 1:
                    Metadata.Instance.currentChapter = "ch2";
                    textP3.GetComponent<TextMeshProUGUI>().text = prompt + "...";
                    Metadata.Instance.previousPrompt = Metadata.Instance.startingPrompt;
                    Metadata.Instance.currentPrompt = prompt;
                    break;
                case 3:
                    Metadata.Instance.currentChapter = "ch3";
                    textP5.GetComponent<TextMeshProUGUI>().text = prompt + "...";
                    Metadata.Instance.previousPrompt = Metadata.Instance.currentPrompt;
                    Metadata.Instance.currentPrompt = prompt;
                    turnBookPage = true;
                    nextPageFive = true;
                    break;
                case 5:
                    turnBookPage = true;
                    bookFinished = true;
                    finishBook.SetActive(true);
                    break;

            }
        }
        void OnPublishToBook(Sprite sprite, string completion, string description, string newprompt, int index ,byte[] imagebytes)
        {
            switch (book.CurrentPageNumber)
			{
                case 1:
                    textP2.SetActive(false);
                    Metadata.Instance.previousPrompt = Metadata.Instance.startingPrompt;
					
					Metadata.Instance.currentChapter = "ch2";
                    textP1.GetComponent<TextMeshProUGUI>().text = completion;
                    Metadata.Instance.currentPrompt = newprompt;

                    imageP2bytes = imagebytes;
                    imageP2.GetComponent<Image>().sprite = sprite;
                    imageP2.SetActive(true);
                    turnBookPage = true;
                    currentTextGenPage = 1;
                    break;

				case 3:
					textP4.SetActive(false);
                    Metadata.Instance.previousPrompt = Metadata.Instance.currentPrompt;
                    Metadata.Instance.currentChapter = "ch3";
                    textP3.GetComponent<TextMeshProUGUI>().text = completion + "\n\n" + description;
                    Metadata.Instance.currentPrompt = newprompt;
                    imageP4.GetComponent<Image>().sprite = sprite;
                    imageP4bytes = imagebytes;
                    imageP4.SetActive(true);
                    turnBookPage = true;
                    nextPageFive = true;
                    currentTextGenPage = 3;
                    break;

                case 5:
                    textP6.SetActive(false);
                    Metadata.Instance.previousPrompt = Metadata.Instance.currentPrompt;
                    textP5.GetComponent<TextMeshProUGUI>().text = completion + "\n\n" + description;
                    imageP6.GetComponent<Image>().sprite = sprite;
                    imageP6.SetActive(true);
                    imageP6bytes = imagebytes;
                    turnBookPage = true;
					bookFinished = true;
                    currentTextGenPage = 5;
					EventSystem.instance.DisableDrawingScreenEvent();
                    EventSystem.instance.EnableBookNavigatorEvent();
					//EventSystem.instance.EnableOwnershipScreenEvent();
                    finishBook.SetActive(true);

                    break;
            }
            
        }


        void OnCompleteTextRevealed(int c)
		{
            Debug.Log("OnCompleteTextRevealed");
			switch (c)
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
                    break;
				case 2:
                    //DownArrow2.SetActive(true);
                    if (Metadata.singleScreenVersion)
                    {
                        EventSystem.instance.SwitchCameraEvent();
                    }
                    
					
                    break;
				case 3:
                    //turnBookPage = true;
                    //endBook = true;
                    textP4.GetComponent<TextMeshProUGUI>().text = drawPictureText.GetLocalizedString();
                    break;
				case 4:
                    if (Metadata.singleScreenVersion)
                    {
                        EventSystem.instance.SwitchCameraEvent();
                    }
                    break;
				case 5:
                    textP6.GetComponent<TextMeshProUGUI>().text = drawPictureText.GetLocalizedString();
                    break;
				case 6:
                    if (Metadata.singleScreenVersion)
                    {
                        EventSystem.instance.SwitchCameraEvent();

                    }
					
                    break;
				case 7:
                    break;

			}
        }


        protected virtual void OnBookTurnToPageCompleted(EndlessBook.StateEnum fromState, EndlessBook.StateEnum toState, int currentPageNumber)
        {
            Debug.Log("OnBookTurnToPageCompleted: State set to " + toState + ". Current Page Number = " + currentPageNumber);
            if (!bookFinished)
            {
                switch (book.CurrentPageNumber)
                {
                    case 3:
                        if (nextBookPage != 5)
                        {

                            //Metadata.Instance.currentTextPage = 3;
                            //turnBookPage = false;
                            EventSystem.instance.EnableDrawingScreenEvent();
                            EventSystem.instance.DisableBookNavigatorEvent();
                            textP3.GetComponent<TextMeshProUGUI>().text = Metadata.Instance.currentPrompt;
                            nextBookPage = 5;
                        }
                        break;

                    case 5:
                        //Metadata.Instance.currentTextPage = 5;
                        if (nextBookPage == 5)
                        {
                            //turnBookPage = false;
                            EventSystem.instance.EnableDrawingScreenEvent();
                            EventSystem.instance.DisableBookNavigatorEvent();
                            textP5.GetComponent<TextMeshProUGUI>().text = Metadata.Instance.currentPrompt;
                            
                        }
                        break;
                }
            }
            DebugCurrentState();
        }
        void DebugCurrentState()
        {
            Debug.Log("CurrentState: " + book.CurrentState);
            Debug.Log("CurrentPageNumber: " + book.CurrentPageNumber);
        }

        // Calls Tiny-Llama
    IEnumerator GetChapterStories(string prompt, System.Action<string> callback)
    {
        string url = "http://127.0.0.1:8000/api/chat/chapterstories?prompt=" + prompt + "&ch_index=" +Metadata.Instance.currentChapter;
        WWWForm form = new WWWForm();
        //form.headers["Content-Type"] = "multipart/form-data";
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                EventSystem.instance.RestartSceneEvent();
                int count = 0;
                
                while (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(request.error);
                    request = UnityWebRequest.Post(url, form);
                    yield return request.SendWebRequest();
                    count++;
                    if (count > 10)
                    {
                        
                    }
                }
                
                Dictionary<string, string> returnVal = JsonConvert.DeserializeObject
                    <Dictionary<string, string>>(request.downloadHandler.text);
                string generated_description = returnVal["generated_description"].ToString();
                callback(generated_description);
            }
            else
            {
                Dictionary<string, string> returnVal = JsonConvert.DeserializeObject
                    <Dictionary<string, string>>(request.downloadHandler.text);
                string generated_description = returnVal["generated_description"].ToString();
                callback(generated_description);
            }
        
    }

        // Calls Instruct-Blip
        public IEnumerator GetFullSentences(byte[] bytes, float temperature, System.Action<string> callback)
        {
            string url = "http://127.0.0.1:8000/api/chat/fullsentences?prompt=" + Metadata.Instance.previousPrompt + "&temperature=" + temperature;
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", bytes);
            form.headers["Content-Type"] = "multipart/form-data";

            UnityWebRequest request = UnityWebRequest.Post(url, form);
            yield return request.SendWebRequest();

            Dictionary<string, string> returnVal = JsonConvert.DeserializeObject
                <Dictionary<string, string>>(request.downloadHandler.text);
            string generated_description = returnVal["generated_description"].ToString();
            yield return generated_description;
        }
    }

   

}
