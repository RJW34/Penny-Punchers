extends Node

# Explicit software-controller QA entry point. The production Main scene remains intact.
func _ready() -> void:
	$Main.call_deferred("BeginUiSmoke")

func _process(delta: float) -> void:
	$Main.call("TickUiSmoke", delta)
