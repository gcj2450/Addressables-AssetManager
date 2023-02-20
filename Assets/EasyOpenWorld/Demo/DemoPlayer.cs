using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyOpenWorld {
    public class DemoPlayer : MonoBehaviour {
        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            transform.position += Vector3.forward * Input.GetAxis("Vertical") * 100 * Time.deltaTime;
            transform.position += Vector3.right * Input.GetAxis("Horizontal") * 100 * Time.deltaTime;
        }
    }
}