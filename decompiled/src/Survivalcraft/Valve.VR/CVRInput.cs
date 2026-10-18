using System.Runtime.InteropServices;
using System.Text;

namespace Valve.VR;

public class CVRInput
{
	private IVRInput FnTable;

	internal CVRInput(nint pInterface)
	{
		FnTable = (IVRInput)Marshal.PtrToStructure(pInterface, typeof(IVRInput));
	}

	public EVRInputError SetActionManifestPath(string pchActionManifestPath)
	{
		return FnTable.SetActionManifestPath(pchActionManifestPath);
	}

	public EVRInputError GetActionSetHandle(string pchActionSetName, ref ulong pHandle)
	{
		pHandle = 0uL;
		return FnTable.GetActionSetHandle(pchActionSetName, ref pHandle);
	}

	public EVRInputError GetActionHandle(string pchActionName, ref ulong pHandle)
	{
		pHandle = 0uL;
		return FnTable.GetActionHandle(pchActionName, ref pHandle);
	}

	public EVRInputError GetInputSourceHandle(string pchInputSourcePath, ref ulong pHandle)
	{
		pHandle = 0uL;
		return FnTable.GetInputSourceHandle(pchInputSourcePath, ref pHandle);
	}

	public EVRInputError UpdateActionState(VRActiveActionSet_t[] pSets, uint unSizeOfVRSelectedActionSet_t)
	{
		return FnTable.UpdateActionState(pSets, unSizeOfVRSelectedActionSet_t, (uint)pSets.Length);
	}

	public EVRInputError GetDigitalActionData(ulong action, ref InputDigitalActionData_t pActionData, uint unActionDataSize, ulong ulRestrictToDevice)
	{
		return FnTable.GetDigitalActionData(action, ref pActionData, unActionDataSize, ulRestrictToDevice);
	}

	public EVRInputError GetAnalogActionData(ulong action, ref InputAnalogActionData_t pActionData, uint unActionDataSize, ulong ulRestrictToDevice)
	{
		return FnTable.GetAnalogActionData(action, ref pActionData, unActionDataSize, ulRestrictToDevice);
	}

	public EVRInputError GetPoseActionDataRelativeToNow(ulong action, ETrackingUniverseOrigin eOrigin, float fPredictedSecondsFromNow, ref InputPoseActionData_t pActionData, uint unActionDataSize, ulong ulRestrictToDevice)
	{
		return FnTable.GetPoseActionDataRelativeToNow(action, eOrigin, fPredictedSecondsFromNow, ref pActionData, unActionDataSize, ulRestrictToDevice);
	}

	public EVRInputError GetPoseActionDataForNextFrame(ulong action, ETrackingUniverseOrigin eOrigin, ref InputPoseActionData_t pActionData, uint unActionDataSize, ulong ulRestrictToDevice)
	{
		return FnTable.GetPoseActionDataForNextFrame(action, eOrigin, ref pActionData, unActionDataSize, ulRestrictToDevice);
	}

	public EVRInputError GetSkeletalActionData(ulong action, ref InputSkeletalActionData_t pActionData, uint unActionDataSize)
	{
		return FnTable.GetSkeletalActionData(action, ref pActionData, unActionDataSize);
	}

	public EVRInputError GetBoneCount(ulong action, ref uint pBoneCount)
	{
		pBoneCount = 0u;
		return FnTable.GetBoneCount(action, ref pBoneCount);
	}

	public EVRInputError GetBoneHierarchy(ulong action, int[] pParentIndices)
	{
		return FnTable.GetBoneHierarchy(action, pParentIndices, (uint)pParentIndices.Length);
	}

	public EVRInputError GetBoneName(ulong action, int nBoneIndex, StringBuilder pchBoneName, uint unNameBufferSize)
	{
		return FnTable.GetBoneName(action, nBoneIndex, pchBoneName, unNameBufferSize);
	}

	public EVRInputError GetSkeletalReferenceTransforms(ulong action, EVRSkeletalTransformSpace eTransformSpace, EVRSkeletalReferencePose eReferencePose, VRBoneTransform_t[] pTransformArray)
	{
		return FnTable.GetSkeletalReferenceTransforms(action, eTransformSpace, eReferencePose, pTransformArray, (uint)pTransformArray.Length);
	}

	public EVRInputError GetSkeletalTrackingLevel(ulong action, ref EVRSkeletalTrackingLevel pSkeletalTrackingLevel)
	{
		return FnTable.GetSkeletalTrackingLevel(action, ref pSkeletalTrackingLevel);
	}

	public EVRInputError GetSkeletalBoneData(ulong action, EVRSkeletalTransformSpace eTransformSpace, EVRSkeletalMotionRange eMotionRange, VRBoneTransform_t[] pTransformArray)
	{
		return FnTable.GetSkeletalBoneData(action, eTransformSpace, eMotionRange, pTransformArray, (uint)pTransformArray.Length);
	}

	public EVRInputError GetSkeletalSummaryData(ulong action, EVRSummaryType eSummaryType, ref VRSkeletalSummaryData_t pSkeletalSummaryData)
	{
		return FnTable.GetSkeletalSummaryData(action, eSummaryType, ref pSkeletalSummaryData);
	}

	public EVRInputError GetSkeletalBoneDataCompressed(ulong action, EVRSkeletalMotionRange eMotionRange, nint pvCompressedData, uint unCompressedSize, ref uint punRequiredCompressedSize)
	{
		punRequiredCompressedSize = 0u;
		return FnTable.GetSkeletalBoneDataCompressed(action, eMotionRange, pvCompressedData, unCompressedSize, ref punRequiredCompressedSize);
	}

	public EVRInputError DecompressSkeletalBoneData(nint pvCompressedBuffer, uint unCompressedBufferSize, EVRSkeletalTransformSpace eTransformSpace, VRBoneTransform_t[] pTransformArray)
	{
		return FnTable.DecompressSkeletalBoneData(pvCompressedBuffer, unCompressedBufferSize, eTransformSpace, pTransformArray, (uint)pTransformArray.Length);
	}

	public EVRInputError TriggerHapticVibrationAction(ulong action, float fStartSecondsFromNow, float fDurationSeconds, float fFrequency, float fAmplitude, ulong ulRestrictToDevice)
	{
		return FnTable.TriggerHapticVibrationAction(action, fStartSecondsFromNow, fDurationSeconds, fFrequency, fAmplitude, ulRestrictToDevice);
	}

	public EVRInputError GetActionOrigins(ulong actionSetHandle, ulong digitalActionHandle, ulong[] originsOut)
	{
		return FnTable.GetActionOrigins(actionSetHandle, digitalActionHandle, originsOut, (uint)originsOut.Length);
	}

	public EVRInputError GetOriginLocalizedName(ulong origin, StringBuilder pchNameArray, uint unNameArraySize, int unStringSectionsToInclude)
	{
		return FnTable.GetOriginLocalizedName(origin, pchNameArray, unNameArraySize, unStringSectionsToInclude);
	}

	public EVRInputError GetOriginTrackedDeviceInfo(ulong origin, ref InputOriginInfo_t pOriginInfo, uint unOriginInfoSize)
	{
		return FnTable.GetOriginTrackedDeviceInfo(origin, ref pOriginInfo, unOriginInfoSize);
	}

	public EVRInputError ShowActionOrigins(ulong actionSetHandle, ulong ulActionHandle)
	{
		return FnTable.ShowActionOrigins(actionSetHandle, ulActionHandle);
	}

	public EVRInputError ShowBindingsForActionSet(VRActiveActionSet_t[] pSets, uint unSizeOfVRSelectedActionSet_t, ulong originToHighlight)
	{
		return FnTable.ShowBindingsForActionSet(pSets, unSizeOfVRSelectedActionSet_t, (uint)pSets.Length, originToHighlight);
	}

	public bool IsUsingLegacyInput()
	{
		return FnTable.IsUsingLegacyInput();
	}
}
