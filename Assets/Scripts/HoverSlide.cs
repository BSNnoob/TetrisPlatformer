using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverSlide : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slide Settings")]
    [SerializeField] private RectTransform pieceToMove;
    [SerializeField] private Vector2 hiddenPosition;
    [SerializeField] private Vector2 visiblePosition;
    [SerializeField] private float slideSpeed = 5f;

    [Header("Main Bar Sprite Animation")]
    [SerializeField] private Image imageToAnimate;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float frameRate = 0.1f;

    [Header("Small Piece Sprite Animation")]
    [SerializeField] private Image smallPieceImage;
    [SerializeField] private Sprite[] smallPieceFrames;
    [SerializeField] private float smallPieceFrameRate = 0.1f;

    private Vector2 targetPos;
    private bool isHovered = false;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool isAnimating = false;
    
    private int smallPieceCurrentFrame = 0;
    private float smallPieceFrameTimer = 0f;
    private bool smallPieceIsAnimating = false;

    void Start()
    {
        targetPos = hiddenPosition;
        pieceToMove.anchoredPosition = hiddenPosition;
        
        if (imageToAnimate != null && animationFrames.Length > 0)
        {
            imageToAnimate.sprite = animationFrames[0];
        }
        
        if (smallPieceImage != null && smallPieceFrames.Length > 0)
        {
            smallPieceImage.sprite = smallPieceFrames[0];
        }
    }

    void Update()
    {
        pieceToMove.anchoredPosition = Vector2.Lerp(
            pieceToMove.anchoredPosition, 
            targetPos, 
            Time.deltaTime * slideSpeed
        );

        if (isAnimating && animationFrames.Length > 0)
        {
            frameTimer += Time.deltaTime;
            
            if (frameTimer >= frameRate)
            {
                frameTimer = 0f;
                
                if (isHovered)
                {
                    currentFrame++;
                    if (currentFrame >= animationFrames.Length)
                    {
                        currentFrame = animationFrames.Length - 1;
                        isAnimating = false;
                    }
                }
                else
                {
                    currentFrame--;
                    if (currentFrame < 0)
                    {
                        currentFrame = 0;
                        isAnimating = false;
                    }
                }
                
                imageToAnimate.sprite = animationFrames[currentFrame];
            }
        }

        if (smallPieceIsAnimating && smallPieceFrames.Length > 0)
        {
            smallPieceFrameTimer += Time.deltaTime;
            
            if (smallPieceFrameTimer >= smallPieceFrameRate)
            {
                smallPieceFrameTimer = 0f;
                
                if (isHovered)
                {
                    smallPieceCurrentFrame++;
                    if (smallPieceCurrentFrame >= smallPieceFrames.Length)
                    {
                        smallPieceCurrentFrame = smallPieceFrames.Length - 1;
                        smallPieceIsAnimating = false;
                    }
                }
                else
                {
                    smallPieceCurrentFrame--;
                    if (smallPieceCurrentFrame < 0)
                    {
                        smallPieceCurrentFrame = 0;
                        smallPieceIsAnimating = false;
                    }
                }
                
                smallPieceImage.sprite = smallPieceFrames[smallPieceCurrentFrame];
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPos = visiblePosition;
        isHovered = true;
        isAnimating = true;
        smallPieceIsAnimating = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPos = hiddenPosition;
        isHovered = false;
        isAnimating = true;
        smallPieceIsAnimating = true;
    }
}