namespace Valve.VR;

public enum VROverlayFlags
{
	None,
	Curved,
	RGSS4X,
	NoDashboardTab,
	AcceptsGamepadEvents,
	ShowGamepadFocus,
	SendVRDiscreteScrollEvents,
	SendVRTouchpadEvents,
	ShowTouchPadScrollWheel,
	TransferOwnershipToInternalProcess,
	SideBySide_Parallel,
	SideBySide_Crossed,
	Panorama,
	StereoPanorama,
	SortWithNonSceneOverlays,
	VisibleInDashboard,
	MakeOverlaysInteractiveIfVisible,
	SendVRSmoothScrollEvents
}
