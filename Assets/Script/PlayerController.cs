using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public bool isMoving;
    public Vector2 input;
    private Animator animator;
    private PhysicsCheck physics;
    public LayerMask intractableLayer;
    
    // 新增：独立记录最后朝向
    private Vector2 lastFacingDirection = Vector2.right;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        physics = GetComponent<PhysicsCheck>();
    }

    public void HandleUpdate()
    {
        if (physics.touchWall)
        {
            isMoving = false;
        }
        
        // 更新朝向
        int faceDir = (int)transform.localScale.x;
        if (input.x > 0) faceDir = 1;
        if (input.x < 0) faceDir = -1;
        transform.localScale = new Vector3(faceDir, 1, 1);
         
        if (!isMoving)
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");

            if (input.x != 0) input.y = 0;
            
            if (input != Vector2.zero)
            {  
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                
                // 新增：更新最后朝向
                UpdateFacingDirection(input);
                
                var targetPos = transform.position;
               
                if (input.x > 0) input.x = 0.5f;
                if (input.y > 0) input.y = 0.5f;
                if (input.x < 0) input.x = -0.5f;
                if (input.y < 0) input.y = -0.5f;
                
                targetPos.x += input.x;
                targetPos.y += input.y;

                StartCoroutine(Move(targetPos));
            }
        }
        animator.SetBool("isMoving", isMoving);
        
        if(Input.GetKeyDown(KeyCode.F)) 
        {
            Interacte();
        }
    }

    // 新增：更新朝向的方法
    private void UpdateFacingDirection(Vector2 direction)
    {
        if (direction != Vector2.zero)
        {
            lastFacingDirection = direction.normalized;
        }
    }

    void Interacte()
    {
        
        var facingDir = lastFacingDirection;
        var interactPos = transform.position + (Vector3)facingDir * 0.6f; 
        
      
        var collider = Physics2D.OverlapCircle(interactPos, 0.2f, intractableLayer);
        
        if (collider != null)
        {
            Debug.Log("检测到可交互物体: " + collider.name);
            collider.GetComponent<Interactable>()?.Interact();
        }
        
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            if (physics.touchWall)
            {
                isMoving = false;
                yield break;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
    }

   
   
}