# Player / Movement

Future fully **2D top-down** locomotion, CharacterBody2D collision, speed modifiers and movement modes. Likely walk, sprint, dodge and later swim or climb where appropriate.

Movement should consume normalized intent from `Input/`, effective movement statistics and short-term stamina from `Survival/Needs/Stamina/`. Reusable collision/locomotion components may eventually serve other actors. Avoid using the retired Camera3D/XZ slope/gravity prototype.

**Current state:** Documentation/folder scaffolding only. The runnable test movement code remains self-contained in `DEBUG/TopDownTest/TestPlayerMovement.cs`, not here.
