using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.AdminModels;

public class GetPolicy_Admin : MonoBehaviour
{
    #if UNITY_EDITOR
    string titleID = "C7881";

    public bool getPolicy = false;
    public bool setPolicy = false;

    public int policyVersion = 0;

    public PermissionStatement statementToAdd;

    public void Update()
    {
        if (getPolicy)
        {
            getPolicy = false;
            GetPolicyRequest request = new GetPolicyRequest();
            request.PolicyName = "ApiPolicy";
            PlayFabAdminAPI.GetPolicy(request, OnGetPolicy, OnFailedGetPolicy);
        }

        if (setPolicy)
        {
            setPolicy = false;
            UpdatePolicyRequest request = new UpdatePolicyRequest();
            request.OverwritePolicy = false;
            request.PolicyName = "ApiPolicy";
            request.PolicyVersion = policyVersion;
            request.Statements = new List<PermissionStatement>();
            statementToAdd.ApiConditions = new ApiCondition();
            statementToAdd.ApiConditions.HasSignatureOrEncryption = Conditionals.Any;
            request.Statements.Add(statementToAdd);
            PlayFabAdminAPI.UpdatePolicy(request, OnUpdatePolicy, OnFailedUpdatePolicy);
        }
    }

    public void OnGetPolicy(GetPolicyResponse response)
    {
        policyVersion = response.PolicyVersion;
        for (int i = 0; i < response.Statements.Count; i++)
        {
            Debug.Log("Action: " + response.Statements[i].Action + " - Conditions: " + response.Statements[i].ApiConditions + " - Comment: " + response.Statements[i].Comment +
                      " - Effect: " + response.Statements[i].Effect + " - Resource: " + response.Statements[i].Resource + " - Principal: " + response.Statements[i].Principal);
        }
    }

    public void OnFailedGetPolicy(PlayFabError error)
    {
        Debug.Log("Get Failed");
    }

    public void OnUpdatePolicy(UpdatePolicyResponse response)
    {
        Debug.Log("Update Succeeded");
    }

    public void OnFailedUpdatePolicy(PlayFabError error)
    {
        Debug.Log("Update Failed " + error.ErrorMessage);
    }
    #endif
}
