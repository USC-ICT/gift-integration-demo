/// <summary>
/// Constrictsd the GUI texture aspect ratio regardless of screen dimensions or aspect ratio.
/// </summary>
using UnityEngine;
using System.Collections;

public class AGConstrictGuiTextureAspectRatio : MonoBehaviour
{
    
    public Vector2 guiTextureOriginalDimensions = new Vector2(1920, 1080);
    //Only one of the following should be true at a time
    public bool constrainTextureToOriginalDimensions = false;
    public bool constrainTextureAspectRatio = true;
    
    //GUITexture guiTextureCmp;
    Vector2 screenDimensions;
    Vector2 textureToScreenRatio;
    Vector2 newTextureDimensions;
    Vector2 adjustedTextureDimensions;
    
    void Start()
    {
        if (constrainTextureToOriginalDimensions == true && constrainTextureAspectRatio == true)
        {
            Debug.LogError("Both constrainTextureToOriginalDimensions and constrainTextureAspectRatio variables are set to true. Only one can be true at a time. Script execution halted.");
            return;
        }
        
        this.gameObject.transform.position = new Vector3(0.5f, 0.5f, 0f);
        
        //guiTextureCmp = this.gameObject.GetComponent<GUITexture>();
        screenDimensions = new Vector2(Screen.width, Screen.height);
        
        //Calculate resized texture to screen ratio
        textureToScreenRatio = new Vector2( screenDimensions.x / guiTextureOriginalDimensions.x, 
                                            screenDimensions.y / guiTextureOriginalDimensions.y
                                            );

        //Call methods based on how this should be constrained
        if (constrainTextureToOriginalDimensions == true){
            m_constrainTextureToOriginalDimensions();
        }
        else if (constrainTextureAspectRatio == true){
            m_constrainTextureAspectRatio();
        }

    }
    
    void m_constrainTextureToOriginalDimensions(){
        Debug.LogWarning("This is not yet functional.");
        return;
        /*
        //This constrains the image to always be at its original dimensions
        newTextureDimensions = new Vector2( guiTextureOriginalDimensions.x * textureToScreenRatio.x, 
                                            guiTextureOriginalDimensions.y * textureToScreenRatio.y
                                            );
        
        guiTextureCmp.pixelInset = new Rect(screenDimensions.x / 2 - newTextureDimensions.x / 2,
                                            screenDimensions.y /2 - newTextureDimensions.y / 2,
                                            (screenDimensions.x / 2 - newTextureDimensions.x / 2) * -2,
                                            (screenDimensions.y /2 - newTextureDimensions.y / 2) * -2
                                            );
        */
    }
    
    public void m_constrainTextureAspectRatio(){
        //Constrain the side that is more affected
        adjustedTextureDimensions = new Vector2(guiTextureOriginalDimensions.x * textureToScreenRatio.y,
                                                guiTextureOriginalDimensions.y * textureToScreenRatio.x
                                                );
        
        if (textureToScreenRatio.x > textureToScreenRatio.y)
        {
            GetComponent<GUITexture>().pixelInset = new Rect(screenDimensions.x / 2 - adjustedTextureDimensions.x / 2,
                                                0f,
                                                (screenDimensions.x / 2 - adjustedTextureDimensions.x / 2) * -2,
                                                0f
                                                );
        }
        else 
        {
            GetComponent<GUITexture>().pixelInset = new Rect(0f,
                                                screenDimensions.y /2 - adjustedTextureDimensions.y / 2,
                                                0f,
                                                (screenDimensions.y /2 - adjustedTextureDimensions.y / 2) * -2
                                                );
        }
    }
    
}
