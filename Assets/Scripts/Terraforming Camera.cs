using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    Vector3 _hitPoint;
    Camera _cam;
    [SerializeField] public float BrushSize = 2f;

    private void Awake(){
        _cam = GetComponent<Camera>();
    }

    private void Terraform(bool add){
        RaycastHit hit;

        if (
            Physics.Raycast(_cam.ScreenPointToRay(Input.mousePosition), 
            out hit, 1000)
        ) {
            Chunk hitChunk = hit.collider.gameObject.GetComponent<Chunk>();

            _hitPoint = hit.point;

            print(_hitPoint.ToString());}

    }

    void LateUpdate(){
        
        if(Input.GetMouseButton(0)){
            Terraform(true);
        }
        else if(Input.GetMouseButton(1)){
            Terraform(false);
        }
    }
    
    void OnDrawGizmos(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_hitPoint, BrushSize);
    }
}