# Risk of Rain Sex Mod

It was a foul night when I made this. Rain, thunder, the works.
I think I might have actually been possessed by some foul beast to create this.

Anyway, it's a basic framework for making characters have sex. 
The only thing of substance here is aligning characters properly and making it easy to set up interactive animations.
All submitted animations MUST be looping and interact well with the other animation you insert to the SexAnimation object.

# Documentation

| Class/Enum | Summary |
| :--: | :--: |
| Class: SexAnimation | Stores two animations: one for the top and one for the bottom |
| Enum: CharacterType | Type of character the animation can be used for. CharacterType.Global means it can be used universally, CharacterType.Player means it can be used on the player. Otherwise, just use the name of the entity. |
| Class: SexMod | Contains all the methods for actually letting sex happen |

## SexMod
| Method/Attribute | Summary |
| :--: | :--: |
| Method: Sex | Makes two entities have sex, freezes all enemies in place |
| Method: AddSexAnimation | Adds an animation to the catalog of animations. |
| Attribute: sexAnimations | List of all stored animations |
| Attribute: SEX_TIME | How long sex happens for |

### Sex
| Parameter | Summary |
| :--: | :--: |
| GameObject player | GameObject corresponding to the player or other entity, eg. CommandoBody(Copy) |
| GameObject mate | GameObject player will have sex with, eg. LemurianBody(Copy) |
| Boolean mateIsTop | True if mate is topping (penetrating) the player |

### AddSexAnimation
| Parameter | Summary |
| :--: | :--: |
| SexAnimation animation | animation to add |
| (optional) AnimationClipParams topParams | Parameters for loading the top animation. Unneeded if already loaded animations through CustomEmotesAPI.AddCustomAnimation |
| (optional) AnimationClipParams bottomParams | Parameters for loading the bottom animation. Unneeded if already loaded animations through CustomEmotesAPI.AddCustomAnimation |

## SexAnimation
| Method/Attribute | Summary |
| :--: | :--: |
| Method: isMatchedTypes | checks if inserted types match the types of the animation. If either animation or inserted type is Global, that will be counted as equivalent. |
| Method: SexAnimation | Creates a SexAnimation object |
| Attribute: topName | Name for the topping animation, what you named the clip in unity when you exported it. NOT the file (eg. run.anim), the name of the animation (eg. Run) |
| Attribute: bottomName | Name for the topping animation, what you named the clip in unity when you exported it. NOT the file (eg. run.anim), the name of the animation (eg. Run) |
| Attribute: topCharacterType | The type of character that is included in the topping animation |
| Attribute: bottomCharacterType | The type of character that is included in the bottoming animation |