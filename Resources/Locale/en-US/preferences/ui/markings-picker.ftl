markings-search = Search
-markings-selection = { $selectable ->
    [0] You have no markings remaining.
    [one] You can select one more marking.
   *[other] You can select { $selectable } more markings.
}
markings-limits = { $required ->
    [true] { $count ->
        [-1] Select at least one marking.
        [0] You cannot select any markings, but somehow, you have to? This is a bug.
        [one] Select one marking.
       *[other] Select at least one marking and up to {$count} markings. { -markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Select any number of markings.
        [0] You cannot select any markings.
        [one] Select up to one marking.
       *[other] Select up to {$count} markings. { -markings-selection(selectable: $selectable) }
    }
}
markings-reorder = Reorder markings

humanoid-marking-modifier-respect-limits = Respect limits
humanoid-marking-modifier-respect-group-sex = Respect group & sex restrictions
humanoid-marking-modifier-base-layers = Base layers
humanoid-marking-modifier-enable = Enable
humanoid-marking-modifier-prototype-id = Prototype id:

# Categories

markings-organ-Torso = Torso
markings-organ-Head = Head
markings-organ-ArmLeft = Left Arm
markings-organ-ArmRight = Right Arm
markings-organ-HandRight = Right Hand
markings-organ-HandLeft = Left Hand
markings-organ-LegLeft = Left Leg
markings-organ-LegRight = Right Leg
markings-organ-FootLeft = Left Foot
markings-organ-FootRight = Right Foot
markings-organ-Eyes = Eyes

markings-layer-Special = Special
markings-layer-Tail = Tail
markings-layer-Tail-Moth = Wings
markings-layer-Hair = Hair
markings-layer-FacialHair = Facial Hair
markings-layer-UndergarmentTop = Undershirt
markings-layer-UndergarmentBottom = Underpants
markings-layer-Chest = Chest
markings-layer-Head = Head
markings-layer-Snout = Snout
markings-layer-SnoutCover = Snout (Cover)
markings-layer-HeadSide = Head (Side)
markings-layer-HeadTop = Head (Top)
markings-layer-Eyes = Eyes
markings-layer-RArm = Right Arm
markings-layer-LArm = Left Arm
markings-layer-RHand = Right Hand
markings-layer-LHand = Left Hand
markings-layer-RLeg = Right Leg
markings-layer-LLeg = Left Leg
markings-layer-RFoot = Right Foot
markings-layer-LFoot = Left Foot
markings-layer-Overlay = Overlay
markings-layer-TailOverlay = Overlay


# iss14: keys used by the Goob-ported MarkingPicker/SingleMarkingPicker UI
marking-points-remaining = Markings remaining: {$points}
marking-used = {$marking-name} ({$marking-category})
marking-used-forced = {$marking-name} ({$marking-category}) (forced)
marking-slot-add = Add
marking-slot-remove = Remove
marking-slot = Slot {$number}

markings-add = Add
markings-remove = Remove
markings-rank-up = Up
markings-rank-down = Down
markings-used = Used markings
markings-unused = Unused markings

markings-category-Special = Special
markings-category-Hair = Hair
markings-category-FacialHair = Facial Hair
markings-category-Head = Head
markings-category-HeadTop = Head (Top)
markings-category-HeadSide = Head (Side)
markings-category-Face = Face
markings-category-Snout = Snout
markings-category-Chest = Chest
markings-category-RightArm = Right Arm
markings-category-RightHand = Right Hand
markings-category-LeftArm = Left Arm
markings-category-LeftHand = Left Hand
markings-category-RightLeg = Right Leg
markings-category-RightFoot = Right Foot
markings-category-LeftLeg = Left Leg
markings-category-LeftFoot = Left Foot
markings-category-Arms = Arms
markings-category-Legs = Legs
markings-category-Groin = Groin
markings-category-Wings = Wings
markings-category-Underwear = Underwear
markings-category-Undershirt = Undershirt
markings-category-Tail = Tail
markings-category-Overlay = Overlay
