using UnityEngine;

public class PlayParticle : Interactive
{
    [SerializeField] private ParticleSystem[] particles;

    public override void Action()
    {
        foreach (ParticleSystem particle in particles)
        {
            if (particle.isPlaying) particle.Stop();
            else particle.Play();
        }

        IsWorking = false;
    }

    public override void ResetObject()
    {
        foreach (ParticleSystem particle in particles) 
            particle.Stop();
    }
}