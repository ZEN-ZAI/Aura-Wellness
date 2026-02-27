package handlers

import (
	"net/http"

	"github.com/aura-wellness/chat-service/internal/models"
	"github.com/aura-wellness/chat-service/internal/repository"
	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
)

type WorkspaceHandler struct {
	workspaceRepo *repository.WorkspaceRepo
	memberRepo    *repository.MemberRepo
}

func NewWorkspaceHandler(wr *repository.WorkspaceRepo, mr *repository.MemberRepo) *WorkspaceHandler {
	return &WorkspaceHandler{workspaceRepo: wr, memberRepo: mr}
}

func (h *WorkspaceHandler) RegisterRoutes(rg *gin.RouterGroup) {
	rg.POST("/workspaces", h.Create)
	rg.GET("/workspaces/bu/:buId", h.GetByBuID)
	rg.GET("/workspaces/:id", h.GetByID)
	rg.POST("/workspaces/:id/members", h.AddMember)
	rg.PUT("/workspaces/:id/members/:personId", h.UpdateMemberAccess)
	rg.GET("/workspaces/:id/members", h.ListMembers)
}

func (h *WorkspaceHandler) Create(c *gin.Context) {
	var req struct {
		BuID      uuid.UUID `json:"bu_id" binding:"required"`
		CompanyID uuid.UUID `json:"company_id" binding:"required"`
		Name      string    `json:"name" binding:"required"`
	}
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	workspace, err := h.workspaceRepo.Create(c.Request.Context(), req.BuID, req.CompanyID, req.Name)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusCreated, workspace)
}

func (h *WorkspaceHandler) GetByBuID(c *gin.Context) {
	buID, err := uuid.Parse(c.Param("buId"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid bu_id"})
		return
	}

	workspace, err := h.workspaceRepo.GetByBuID(c.Request.Context(), buID)
	if err != nil {
		if err == pgx.ErrNoRows {
			c.JSON(http.StatusNotFound, gin.H{"error": "workspace not found"})
			return
		}
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, workspace)
}

func (h *WorkspaceHandler) GetByID(c *gin.Context) {
	id, err := uuid.Parse(c.Param("id"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid id"})
		return
	}

	workspace, err := h.workspaceRepo.GetByID(c.Request.Context(), id)
	if err != nil {
		if err == pgx.ErrNoRows {
			c.JSON(http.StatusNotFound, gin.H{"error": "workspace not found"})
			return
		}
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, workspace)
}

func (h *WorkspaceHandler) AddMember(c *gin.Context) {
	workspaceID, err := uuid.Parse(c.Param("id"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid workspace id"})
		return
	}

	var req struct {
		PersonID uuid.UUID `json:"person_id" binding:"required"`
		Role     string    `json:"role" binding:"required"`
	}
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	member, err := h.memberRepo.Add(c.Request.Context(), workspaceID, req.PersonID, req.Role)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusCreated, member)
}

func (h *WorkspaceHandler) UpdateMemberAccess(c *gin.Context) {
	workspaceID, err := uuid.Parse(c.Param("id"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid workspace id"})
		return
	}
	personID, err := uuid.Parse(c.Param("personId"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid person id"})
		return
	}

	var req struct {
		HasAccess bool `json:"has_access"`
	}
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	member, err := h.memberRepo.UpdateAccess(c.Request.Context(), workspaceID, personID, req.HasAccess)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, member)
}

func (h *WorkspaceHandler) ListMembers(c *gin.Context) {
	workspaceID, err := uuid.Parse(c.Param("id"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid workspace id"})
		return
	}

	members, err := h.memberRepo.ListByWorkspace(c.Request.Context(), workspaceID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	if members == nil {
		members = []*models.ChatWorkspaceMember{}
	}
	c.JSON(http.StatusOK, members)
}
